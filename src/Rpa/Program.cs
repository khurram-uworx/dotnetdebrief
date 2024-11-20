using FlaUI.Core;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Rpa;
using OperatingSystem = FlaUI.Core.Tools.OperatingSystem;

//Application application = null;
//if (OperatingSystem.IsWindows10())
//    // Use the store application on those systems
//    application = Application.LaunchStoreApp("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App");
//else if (OperatingSystem.IsWindowsServer2016() || OperatingSystem.IsWindowsServer2019())
//    // The calc.exe on this system is just a stub which launches win32calc.exe
//    application = Application.Launch("win32calc.exe");
//else
//    application = Application.Launch("calc.exe");

//Application application = OperatingSystem.IsWindows10()
//    ? Application.LaunchStoreApp("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App")
//    : OperatingSystem.IsWindowsServer2016() || OperatingSystem.IsWindowsServer2019()
//        ? Application.Launch("win32calc.exe")
//        : Application.Launch("calc.exe");

Application application = OperatingSystem.IsWindows10() switch
{
    true => Application.LaunchStoreApp("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"),
    false when OperatingSystem.IsWindowsServer2016() || OperatingSystem.IsWindowsServer2019() => Application.Launch("win32calc.exe"),
    _ => Application.Launch("calc.exe")
};

var window = application.GetMainWindow(new UIA3Automation());
var calc = (OperatingSystem.IsWindows10() || OperatingSystem.IsWindows11()) ? (ICalculator)new Win10Calc(window) : new LegacyCalc(window);

// Switch to default mode
Thread.Sleep(1000);
Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.KEY_1);
Wait.UntilInputIsProcessed();
application.WaitWhileBusy();
Thread.Sleep(1000);

// Simple addition
calc.Button1.Click();
calc.Button2.Click();
calc.Button3.Click();
calc.Button4.Click();
calc.ButtonAdd.Click();
calc.Button5.Click();
calc.Button6.Click();
calc.Button7.Click();
calc.Button8.Click();
calc.ButtonEquals.Click();
application.WaitWhileBusy();
var result = calc.Result;

//https://github.com/FlaUI/FlaUI/blob/master/src/FlaUI.Core.UITests/CalculatorTests.cs
//Assert.That(result, Is.EqualTo("6912"));

Console.WriteLine($"Result from Windows Calculator: {result}, press enter to close it!");
Console.ReadLine();
application.Close();