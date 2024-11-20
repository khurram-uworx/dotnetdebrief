using FlaUI.Core.AutomationElements;
using System.Text.RegularExpressions;

namespace Rpa;

public interface ICalculator
{
    Button Button1 { get; }
    Button Button2 { get; }
    Button Button3 { get; }
    Button Button4 { get; }
    Button Button5 { get; }
    Button Button6 { get; }
    Button Button7 { get; }
    Button Button8 { get; }
    Button ButtonAdd { get; }
    Button ButtonEquals { get; }
    string Result { get; }
}

public class LegacyCalc : ICalculator
{
    private readonly AutomationElement _mainWindow;

    public Button Button1 => FindElement("1").AsButton();
    public Button Button2 => FindElement("2").AsButton();
    public Button Button3 => FindElement("3").AsButton();
    public Button Button4 => FindElement("4").AsButton();
    public Button Button5 => FindElement("5").AsButton();
    public Button Button6 => FindElement("6").AsButton();
    public Button Button7 => FindElement("7").AsButton();
    public Button Button8 => FindElement("8").AsButton();
    public Button ButtonAdd => FindElement("Add").AsButton();
    public Button ButtonEquals => FindElement("Equals").AsButton();

    public string Result
    {
        get
        {
            var resultElement = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("158"));
            var value = resultElement.Properties.Name;
            return Regex.Replace(value, "[^0-9]", String.Empty);
        }
    }

    public LegacyCalc(AutomationElement mainWindow)
    {
        _mainWindow = mainWindow;
    }

    private AutomationElement FindElement(string text)
    {
        var element = _mainWindow.FindFirstDescendant(cf => cf.ByText(text));
        return element;
    }
}

public class Win10Calc : ICalculator
{
    private readonly AutomationElement _mainWindow;

    public Button Button1 => FindElement("num1Button").AsButton();
    public Button Button2 => FindElement("num2Button").AsButton();
    public Button Button3 => FindElement("num3Button").AsButton();
    public Button Button4 => FindElement("num4Button").AsButton();
    public Button Button5 => FindElement("num5Button").AsButton();
    public Button Button6 => FindElement("num6Button").AsButton();
    public Button Button7 => FindElement("num7Button").AsButton();
    public Button Button8 => FindElement("num8Button").AsButton();
    public Button ButtonAdd => FindElement("plusButton").AsButton();
    public Button ButtonEquals => FindElement("equalButton").AsButton();

    public string Result
    {
        get
        {
            var resultElement = FindElement("CalculatorResults");
            var value = resultElement.Properties.Name;
            return Regex.Replace(value, "[^0-9]", String.Empty);
        }
    }

    public Win10Calc(AutomationElement mainWindow)
    {
        _mainWindow = mainWindow;
    }

    private AutomationElement FindElement(string text)
    {
        var element = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(text));
        return element;
    }
}