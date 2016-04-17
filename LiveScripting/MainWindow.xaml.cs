using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Sodium;

namespace LiveScripting
{
    public partial class MainWindow
    {
        private static readonly StreamSink<string> sDocChanged = new StreamSink<string>();
        private static readonly string saveTarget = $"{System.AppDomain.CurrentDomain.BaseDirectory}tmp.cs";
        private static List<IListener> rglisteners = new List<IListener>();
        
        public MainWindow()
        {
            InitializeComponent();

            
            this.cvsResult.FocusVisualStyle = new Style();

            Transaction.RunVoid(() =>
            {
                // register a handler for document changes; sends new document text to StreamSink sDocChanged 
                txtInput.Document.Changed += DocumentChangedHandler;

                StreamSink<SizeChangedEventArgs> sSize = new StreamSink<SizeChangedEventArgs>();
                gridResult.SizeChanged += (sender, args) => sSize.Send(args);
                Screen.Size = sSize.Accum(new Size(0, 0), (args, size) => args.NewSize);

                StreamSink<RenderingEventArgs> sRenderEvents = new StreamSink<RenderingEventArgs>();
                CompositionTarget.Rendering += (sender, args) => sRenderEvents.Send((RenderingEventArgs) args);
                Time.Ticks = sRenderEvents.Collect(TimeSpan.Zero,
                    (re, t) => Tuple.Create(re.RenderingTime - t, re.RenderingTime));

                StreamSink<MouseEventArgs> sMouseMove = new StreamSink<MouseEventArgs>();
                cvsResult.MouseMove += (sender, args) => sMouseMove.Send(args);
                Cell<Canvas> cCvs = Cell.Constant(cvsResult);

                Mouse.MousePos = sMouseMove.Snapshot(cCvs, (args, cvs) => args.GetPosition(cvs))
                    .Hold(Graphics.PointZero);

                StreamSink<MouseEventArgs> sMouseButton = new StreamSink<MouseEventArgs>();
                cvsResult.MouseDown += (sender, args) => sMouseButton.Send(args);
                cvsResult.MouseUp += (sender, args) => sMouseButton.Send(args);
                Mouse.MouseButtons =
                    sMouseButton.Map(
                        args =>
                            new Tuple<MouseButtonState, MouseButtonState, MouseButtonState>(
                                args.LeftButton, args.MiddleButton, args.RightButton))
                        .Hold(new Tuple<MouseButtonState, MouseButtonState, MouseButtonState>(
                            MouseButtonState.Released, MouseButtonState.Released, MouseButtonState.Released));

                Mouse.MouseButtons.Listen(FocusResultArea);

                StreamSink<KeyEventArgs> sKeys = new StreamSink<KeyEventArgs>();
                cvsResult.KeyDown += (sender, args) => sKeys.Send(args);
                cvsResult.KeyUp += (sender, args) => sKeys.Send(args);

                Keyboard.Arrows = CellDirKeys(sKeys, Key.Up, Key.Right, Key.Down, Key.Left);
                Keyboard.Wasd = CellDirKeys(sKeys, Key.W, Key.D, Key.S, Key.A);

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
                 * look for a main variable that we can render if it holds a drawing
                 */
                Cell<ScriptVariable> cMain =
                    cExecResult.Map(result => GetVariable(result.scriptState, "main"));

                cMain.Listen(HandleMain);
                cExecResult.Listen(HandleError);
            });

            Transaction.RunVoid(() =>
            {
                StreamSink<KeyEventArgs> sKeysInput = new StreamSink<KeyEventArgs>();
                this.txtInput.KeyDown += (sender, args) => sKeysInput.Send(args);
                Stream<Unit> sSaveCommand = sKeysInput.Filter(args => args.IsDown && args.Key == Key.S &&
                                                                      (System.Windows.Input.Keyboard.IsKeyDown(
                                                                          Key.LeftCtrl) ||
                                                                       System.Windows.Input.Keyboard.IsKeyDown(
                                                                           Key.RightCtrl))).Map(_ => Unit.Value);

                sSaveCommand.Listen(_ =>
                {
                    FileStream fs = new FileStream(saveTarget, FileMode.Create);
                    this.txtInput.Save(fs);
                    fs.Close();
                });
            });
        }

        private static Cell<Tuple<int, int>> CellDirKeys(Stream<KeyEventArgs> sKeys, Key keyUp, Key keyRight, Key keyDown, Key keyLeft)
        {
            Stream<KeyEventArgs> sUp = sKeys.Filter(args => args.Key == keyUp);
            Stream<KeyEventArgs> sDown = sKeys.Filter(args => args.Key == keyDown);
            Stream<KeyEventArgs> sLeft = sKeys.Filter(args => args.Key == keyLeft);
            Stream<KeyEventArgs> sRight = sKeys.Filter(args => args.Key == keyRight);

            Cell<int> cX = CellDir(sLeft, sRight);
            Cell<int> cY = CellDir(sUp, sDown);

            return cX.Lift(cY, (x, y) => new Tuple<int, int>(x, y));
        } 

        private static Cell<int> CellDir(Stream<KeyEventArgs> sNeg, Stream<KeyEventArgs> sPos)
        {
            Cell<int> cDec = sNeg.Map(args => args.IsDown ? -1 : 0).Calm().Hold(0);
            Cell<int> cInc = sPos.Map(args => args.IsDown ? 1 : 0).Calm().Hold(0);
            return cDec.Lift(cInc, (dec, inc) => dec + inc);
        }

        private void FocusResultArea(Tuple<MouseButtonState, MouseButtonState, MouseButtonState> tuple)
        {
            if (tuple.Item1 == MouseButtonState.Pressed ||
                tuple.Item2 == MouseButtonState.Pressed ||
                tuple.Item3 == MouseButtonState.Pressed)
                this.cvsResult.Focus();
        }


        private void HandleMain(ScriptVariable main)
        {
            
            if (main == null)
            {
                Draw(new Text("<nothing to render>\nno main variable found"));
                return;
            }

            rglisteners.ForEach(l => l.Unlisten());

            if (main.Type == typeof (Cell<Element>))
            {
                var cElement = main.Value as Cell<Element>;
                if (cElement == null)
                {
                    Draw(new Text("<nothing to render>\nmain variable is of type 'Cell<Element>' but has no value.\nMake sure the value you assign is declared before the main variable!"));
                    return;
                }
                rglisteners.Add(cElement.Listen(Draw));
                return;
            }

            if (main.Type != typeof(Element))
            {
                Draw(new Text($"<nothing to render>\nmain variable must be of type 'Element' or 'Cell<Element>'\nis: {main.Value?.GetType()}"));
                return;
            }
            var element = main.Value as Element;
            if (element == null)
            {
                Draw(new Text("<nothing to render>\nmain variable is of type 'Element' but has no value.\nMake sure the value you assign is declared before the main variable!"));
                return;
            }

            Draw(element);
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

        private void Draw(Element element)
        {
            using (var dc = vh.Dv.RenderOpen())
            {
                element.Draw(dc);
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
                .WithReferences(typeof(Element).Assembly);

            var scriptState =
                await
                    CSharpScript.RunAsync<object>(
                        @"using LiveScripting;
                          using Sodium;
                          using System;
                          using System.Windows;
                          using System.Windows.Media;
                          using System.Collections.Generic;
                          
                          using g = LiveScripting.Graphics;
                          using e = LiveScripting.Graphics.Element;
                          using t = LiveScripting.Transform;
                          using m = LiveScripting.Mouse;
                          using k = LiveScripting.Keyboard;",
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
