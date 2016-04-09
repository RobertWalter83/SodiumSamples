using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Sodium;

namespace LiveScripting
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Transaction.RunVoid(() =>
            {
                var cShowErrors = new CellSink<bool?>(rdbErrors.IsChecked);

                rdbErrors.Checked += (sender, args) => cShowErrors.Send(true);
                rdbErrors.Unchecked += (sender, args) => cShowErrors.Send(false);

                var sTextChanged = new StreamSink<string>();

                txtInput.Document.Changed += (sender, args) =>
                {
                    var textCur = (sender as TextDocument)?.Text;
                    txtInput.Dispatcher.InvokeAsync(() => sTextChanged.Send(textCur));
                };

                CellLoop<ExecutionResult> cExecutionResult = new CellLoop<ExecutionResult>();
                Stream<ExecutionResult> sExecutionResult = sTextChanged.Snapshot(cExecutionResult, cShowErrors, Execute);
                cExecutionResult.Loop(sExecutionResult.Hold(ExecutionResult.Nil));

                cExecutionResult.Listen(executionResult =>
                    txtResult.Text = executionResult.compilerException == null || !executionResult.fShowErrors
                        ? executionResult.taskScriptState?.Result?.ReturnValue?.ToString()
                        : executionResult.compilerException.Message
                );
            });
        }

        private static ExecutionResult Execute(string code, ExecutionResult sse, bool? fShowErrors)
        {
            bool _fShowErrors = !fShowErrors.HasValue || fShowErrors.Value;
            try
            {
                if (sse.taskScriptState == null)
                    return new ExecutionResult(CSharpScript.RunAsync(code), null, _fShowErrors);

                return new ExecutionResult(sse.taskScriptState.Result.ContinueWithAsync(code), null, _fShowErrors);
            }
            catch (CompilationErrorException cee)
            {
                return new ExecutionResult(sse.taskScriptState, cee, _fShowErrors);
            }
        }

        private struct ExecutionResult
        {
            internal readonly Task<ScriptState<object>> taskScriptState;
            internal readonly CompilationErrorException compilerException;
            internal readonly bool fShowErrors;
            internal static readonly ExecutionResult Nil = new ExecutionResult(null, null, true);

            internal ExecutionResult(Task<ScriptState<object>> taskScriptState, CompilationErrorException compilerException, bool fShowErrors)
            {
                this.taskScriptState = taskScriptState;
                this.compilerException = compilerException;
                this.fShowErrors = fShowErrors;
            }
        }
    }
}
