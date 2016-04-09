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
        private static readonly StreamSink<string> sDocChanged = new StreamSink<string>();

        public MainWindow()
        {
            InitializeComponent();
            
            Transaction.RunVoid(() =>
            {
                // register a handler for document changes; sends new document text to StreamSink sDocChanged 
                txtInput.Document.Changed += DocumentChangedHandler;

                /**
                 * we accumulate the state of our C# script on each document change
                 * we start with a "nil" state
                 */
                Cell<ExecutionResult> cExecutionResult = sDocChanged.Accum(ExecutionResult.Nil, Execute);
                
                /**
                 * we interface our FRP logic with WPF
                 * if the execution result doesn't contain a compiler error, 
                   we try to retrieve the result of the script and collapse the error message
                 * if there is an error, we keep showing the last valid result, but highlight the current error
                 */
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

        /**
         * send document changes to our FRP sink, asynchronously.
         */
        private void DocumentChangedHandler(object sender, DocumentChangeEventArgs e)
        {
            var textCur = (sender as TextDocument)?.Text;
            txtInput.Dispatcher.InvokeAsync(() => sDocChanged.Send(textCur));
        }

        /**
         * Ran each time the document changes.
         * Within a session, we keep the scriptstate alive, meaning that you can access variables you declared once but deleted from teh script later.
         * Thankfully, ContinueWithAsync allows us to override the same identifier.
         *
         * In case of a compiler error, we keep the last script state alive and store the compiler error to display it on the screen
         */
        private static ExecutionResult Execute(string code, ExecutionResult executionResult)
        {
            try
            {
                if (executionResult.taskScriptState == null)
                    return new ExecutionResult(CSharpScript.RunAsync(code), null);

                return new ExecutionResult(executionResult.taskScriptState.Result.ContinueWithAsync(code), null);
            }
            catch (CompilationErrorException cee)
            {
                return new ExecutionResult(executionResult.taskScriptState, cee);
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
