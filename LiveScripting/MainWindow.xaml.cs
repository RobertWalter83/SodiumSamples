using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Sodium;

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
                 * we create a constant scriptState that holds all assemblies and usings we want in our editor by default
                 * we accumulate the state of our C# script on each document change, running the new code in
                   context of cScriptState.
                 * the scriptState that gets updated (sExecResult) is initialized with Nil
                 */
                Cell<ScriptState<object>> cScriptStart = Cell.Constant(SetupResultCanvas().Result);
                CellLoop<ExecutionResult> cExecResult = new CellLoop<ExecutionResult>();
                Stream<ExecutionResult> sExecResult = sDocChanged.Snapshot(cExecResult, cScriptStart, Execute);
                cExecResult.Loop(sExecResult.Hold(ExecutionResult.Nil));

                /**
                 * look for a main variable that we can render if it holds a Drawing
                 */
                Cell<ScriptVariable> cMain =
                    cExecResult.Map(result => GetVariable(result.scriptState, "main"));

                cMain.Listen(main =>
                {
                    if (main == null)
                    {
                        DisplayTextResult("<nothing to render: no main variable found>");
                        return;
                    }

                    var element = main.Value as Element;
                    if (element == null)
                    {
                        DisplayTextResult("<nothing to render: main variable must be of type 'Element'");
                        return;
                    }

                    using (var dc = vh.Dv.RenderOpen())
                    {
                        element.Draw(dc);
                    }
                });

                cExecResult.Listen(HandleError);
            });
        }

        private void HandleError(ExecutionResult executionResult)
        {
            if (executionResult.errorMessage == null)
            {
                lblError.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblError.Text = executionResult.errorMessage;
                lblError.Visibility = Visibility.Visible;
            }
        }

        private void DisplayTextResult(string text)
        {
            using (var dc = vh.Dv.RenderOpen())
            {
                dc.DrawText(Graphics.Drawing.Show(text).Text, Graphics.PointZero);
            }
        }

        private static ScriptVariable GetVariable(ScriptState<object> scriptState, string name)
        {
            if (scriptState == null)
                return null;

            foreach (var variable in scriptState.Variables)
            {
                if (variable.Name == name)
                    return variable;
            }
            return null;
        }

        public async Task<ScriptState<object>> SetupResultCanvas()
        {
            var options = ScriptOptions.Default
                .WithReferences(typeof(Setup).Assembly);

            var scriptState =
                await
                    CSharpScript.RunAsync<object>(
                        @"using LiveScripting;",
                        options);

            return scriptState;
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
        private ExecutionResult Execute(string code, ExecutionResult executionResult, ScriptState<object> scriptStart)
        {
            try
            {
                return new ExecutionResult(scriptStart.ContinueWithAsync(code).Result, null);
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
