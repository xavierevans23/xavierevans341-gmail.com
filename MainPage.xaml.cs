using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Assembly_Program
{

    public class FullProgram
    {
        private enum CommandType
        {
            Load,
            Store,
            Input,
            Output,
            Branch,
            BranchZero,
            BranchPositive,
            Data,
            Add,
            Subtract,
            Halt
        }

        private static Dictionary<string, CommandType> CommandTranslation = new Dictionary<string, CommandType>() { { "LDA", CommandType.Load }, { "STA", CommandType.Store }, { "INP", CommandType.Input }, { "OUT", CommandType.Output }, { "BRA", CommandType.Branch }, { "BRZ", CommandType.BranchZero }, { "BRP", CommandType.BranchPositive }, { "DAT", CommandType.Data }, { "ADD", CommandType.Add }, { "SUB", CommandType.Subtract }, { "HLT", CommandType.Halt }, };
        private Dictionary<string, int> Locations;
        private Dictionary<string, int> Variables;
        private string[] Program;

        public int Accumalator
        {
            get
            {
                return accumalator;
            }
            set
            {
                accumlatorWaitingForInput = false;
                accumalator = value;
            }
        }
        private int accumalator = 0;
        private bool accumlatorWaitingForInput = false;
        public bool WaitingForInput
        {
            get
            {
                return accumlatorWaitingForInput;
            }
        }
        public int currentLine = 0;

        private List<int> internalOutput;

        public List<int> Output
        {
            get
            {
                if (internalOutput.Count > 0)
                {
                    return internalOutput.GetRange(0, internalOutput.Count);
                }
                else
                {
                    return new List<int>() { };
                }

            }
        }

        private bool programHalted;

        public bool Halted
        {
            get
            {
                return programHalted;
            }
        }

        public FullProgram(string[] InputProgram)
        {
            accumalator = 0;
            programHalted = false;
            Program = InputProgram;
            accumlatorWaitingForInput = false;
            Locations = new Dictionary<string, int>();
            Variables = new Dictionary<string, int>();
            internalOutput = new List<int>();

            int LineCount = 0;
            foreach (string comm in Program)
            {
                execute(comm, LineCount, true);
                LineCount++;
            }
        }

        public void ExecuteCommand(string command)
        {
            execute(command, -1, false);
            execute(command, -1, true);
        }

        public void ExecuteCommand(string command, int Line)
        {
            execute(command, Line, false);
        }

        public void ExecuteCommand(int Line)
        {
            execute(Program[Line], Line, false);
        }

        public void Run()
        {
            while (programHalted == false && accumlatorWaitingForInput == false)
            {
                currentLine++;
                execute(Program[currentLine - 1], currentLine - 1, false);
            }
        }

        private void execute(string command, int Line, bool checkOnly)
        {
            CommandType commandType;
            string[] commandString = command.Split(' ');
            int Offset = 0;
            if (CommandTranslation.ContainsKey(commandString[0]))
            {
                commandType = CommandTranslation[commandString[0]];
            }
            else
            {
                Offset++;
                commandType = CommandTranslation[commandString[1]];
                if (checkOnly)
                {
                    if (commandType == CommandType.Data)
                    {
                        if (commandString.Length == 3)
                        {
                            Variables.Add(commandString[0], Convert.ToInt32(commandString[2]));
                        }
                        else
                        {
                            Variables.Add(commandString[0], 0);
                        }
                    }
                    else
                    {
                        if (Line != -1)
                        {
                            Locations.Add(commandString[0], Line);
                        }
                    }
                }

            }
            if (!checkOnly)
            {
                string SecondArgument = "";
                if (commandString.Length > 1 + Offset)
                {
                    SecondArgument = commandString[1 + Offset];
                }

                switch (commandType)
                {
                    case (CommandType.Add):
                        accumalator += Variables[SecondArgument];
                        break;
                    case (CommandType.Subtract):
                        accumalator -= Variables[SecondArgument];
                        break;
                    case (CommandType.Branch):
                        currentLine = Locations[SecondArgument];
                        break;
                    case (CommandType.BranchPositive):
                        if (accumalator >= 0)
                        {
                            currentLine = Locations[SecondArgument];
                        }
                        break;
                    case (CommandType.BranchZero):
                        if (accumalator == 0)
                        {
                            currentLine = Locations[SecondArgument];
                        }
                        break;
                    case (CommandType.Input):
                        accumlatorWaitingForInput = true;
                        break;
                    case (CommandType.Output):
                        internalOutput.Add(accumalator);
                        break;
                    case (CommandType.Load):
                        accumalator = Variables[SecondArgument];
                        break;
                    case (CommandType.Store):
                        Variables[SecondArgument] = accumalator;
                        break;
                    case (CommandType.Halt):
                        programHalted = true;
                        break;
                }
            }
        }
    }

    public sealed partial class MainPage : Page
    {

        FullProgram pro;
        

        public MainPage()
        {
            this.InitializeComponent();
            pro = new FullProgram(new string[] { });
        }

        private void ProgramButton(object sender, RoutedEventArgs e)
        {
            pro.Run();
            OutputBox.Text = ConvertOutput(pro.Output);
        }

        string ConvertOutput(List<int> input)
        {
            string outputText = "";
            foreach (int line in input)
            {

                outputText += line.ToString();
                outputText += "\n\r";
            }
            if (pro.WaitingForInput)
            {
                outputText += "Enter number: ";
            }
            return outputText;
        }

        private void InputButton(object sender, RoutedEventArgs e)
        {
            if (pro.WaitingForInput)
            {
                pro.Accumalator = Convert.ToInt32(CommandBox.Text);
                pro.Run();
            }
            else
            {
                pro.ExecuteCommand(CommandBox.Text);
            }
            CommandBox.Text = "";
            OutputBox.Text = ConvertOutput(pro.Output);
        }

        private void LineNumber_TextChanged(object sender, TextChangedEventArgs e)
        {

            try
            {
                pro.currentLine = Convert.ToInt32(LineNumber.Text);
            }
            catch
            {

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            pro.ExecuteCommand(pro.currentLine);
            pro.currentLine++;
            LineNumber.Text = pro.currentLine.ToString();
            OutputBox.Text = ConvertOutput(pro.Output);
        }

        private void ProgramBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string[] ProgramInput = ProgramBox.Text.Split(new[] { '\r', '\n' });
            try
            {
                pro = new FullProgram(ProgramInput);
                Success.Text = "No errors";
            }
            catch
            {
                Success.Text = "So many errors";
            }
            
        }
    }
}

