using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            txtInput.AcceptsTab = true;
            txtInput.AcceptsReturn = true;
            txtInput.TextWrapping = TextWrapping.Wrap;

            Transaction.RunVoid(() =>
            {
                var sTextChanged = new StreamSink<string>();

                txtInput.TextChanged += (sender, args) =>
                {
                    var textCur = (args.Source as TextBox)?.Text;
                    txtInput.Dispatcher.InvokeAsync(() => sTextChanged.Send(textCur));
                };

                Cell<ScriptStateExtended> cScriptState = sTextChanged.Accum(ScriptStateExtended.Nil, Execute);

                cScriptState.Listen(sse =>
                    txtResult.Text = sse.cee == null
                        ? sse.scriptState?.Result?.ReturnValue?.ToString()
                        : sse.cee.Message
                );
            });
        }

        private static ScriptStateExtended Execute(string code, ScriptStateExtended sse)
        {
            try
            {
                if (sse.scriptState == null)
                    return new ScriptStateExtended(CSharpScript.RunAsync(code), null);

                return new ScriptStateExtended(sse.scriptState.Result.ContinueWithAsync(code), null);
            }
            catch (CompilationErrorException cee)
            {
                return new ScriptStateExtended(sse.scriptState, cee);
            }
        }

        private struct ScriptStateExtended
        {
            internal readonly Task<ScriptState<object>> scriptState;
            internal readonly CompilationErrorException cee;
            internal static readonly ScriptStateExtended Nil = new ScriptStateExtended(null, null);

            internal ScriptStateExtended(Task<ScriptState<object>> scriptState, CompilationErrorException cee)
            {
                this.scriptState = scriptState;
                this.cee = cee;
            }
        }
    }
}
