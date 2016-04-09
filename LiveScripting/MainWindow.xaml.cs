using System;
using System.Linq.Expressions;
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
                    if (executionResult.errorMessage == null)
                    {
                        txtResult.Text = executionResult.scriptState?.ReturnValue?.ToString();
                        lblError.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        lblError.Text = executionResult.errorMessage;
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
         * we always re-run the whole script, currently. ScriptState.ContinueWithAsync is an option, but it would run
         * "intermediate steps" of the script as well, which can have weird side effects for the user.
         * In case of a compiler error, we keep the last script state alive and store the compiler error to display it on the screen
         */
        private static ExecutionResult Execute(string code, ExecutionResult executionResult)
        {
            try
            {
                return new ExecutionResult(CSharpScript.RunAsync(code).Result, null);
            }
            catch (Exception ex)
            {
                string errorMessage;
                if (ex is AggregateException && ex.InnerException != null)
                    errorMessage = "Runtime error: " + ex.InnerException.Message;
                else if (ex is CompilationErrorException)
                    errorMessage = "Compile time error: " + ex.Message;
                else
                    errorMessage = "Unknown error: " + ex.Message;

                return new ExecutionResult(executionResult.scriptState, errorMessage);
            }
        }

        private struct ExecutionResult
        {
            internal readonly ScriptState<object> scriptState;
            internal readonly string errorMessage;
            internal static readonly ExecutionResult Nil = new ExecutionResult(null, null);

            internal ExecutionResult(ScriptState<object> scriptState, string errorMessage)
            {
                this.scriptState = scriptState;
                this.errorMessage = errorMessage;
            }
        }
    }
}
