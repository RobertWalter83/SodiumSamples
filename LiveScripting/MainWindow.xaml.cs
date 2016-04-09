using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Sodium;
using Label = System.Reflection.Emit.Label;

namespace LiveScripting
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            Transaction.RunVoid(() =>
            {
                var sTextChanged = new StreamSink<string>();

                txtInput.Document.Changed += (sender, args) =>
                {
                    var textCur = (sender as TextDocument)?.Text;
                    txtInput.Dispatcher.InvokeAsync(() => sTextChanged.Send(textCur));
                };

                Cell<ExecutionResult> cExecutionResult = sTextChanged.Accum(ExecutionResult.Nil, Execute);
                
                cExecutionResult.Listen(executionResult =>
                {
                    if (executionResult.compilerException == null)
                    {
                        txtResult.Text = executionResult.taskScriptState?.Result?.ReturnValue?.ToString();
                        lblError.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        lblError.Text = executionResult.compilerException.Message;
                        lblError.Visibility = Visibility.Visible;
                    }
                });
            });
        }

        private static ExecutionResult Execute(string code, ExecutionResult sse)
        {
            try
            {
                if (sse.taskScriptState == null)
                    return new ExecutionResult(CSharpScript.RunAsync(code), null);

                return new ExecutionResult(sse.taskScriptState.Result.ContinueWithAsync(code), null);
            }
            catch (CompilationErrorException cee)
            {
                return new ExecutionResult(sse.taskScriptState, cee);
            }
        }

        private struct ExecutionResult
        {
            internal readonly Task<ScriptState<object>> taskScriptState;
            internal readonly CompilationErrorException compilerException;
            internal static readonly ExecutionResult Nil = new ExecutionResult(null, null);

            internal ExecutionResult(Task<ScriptState<object>> taskScriptState, CompilationErrorException compilerException)
            {
                this.taskScriptState = taskScriptState;
                this.compilerException = compilerException;
            }
        }
    }
}
