using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace SosLibraryArchiver
{
  public static class RegistryHelper
  {
    #region Constants

    private const string ContextMenuRegistrationValueName = "ContextMenuRegistrationEnabled";

    private const string ShellApplicationPath = @"dllfile\shell\zArchive SOS Library";
    private const string ShellCommandPath = ShellApplicationPath + "\\command";
    private const string ToolSubKeyPath = "Software\\Zvirja\\LibraryArchiver";

    #endregion

    #region Public Methods and Operators

    public static void EnsureContextMenuRegistrationConsistent(string pathToExe)
    {
      if (!GetRegistrationIsSwitchedOn())
      {
        RemoveContextMenuRegistration();
        return;
      }

      bool validValueIsPresent = false;
      string shellCommandValue = GetShellCommandValue(pathToExe);
      RegistryKey commandKey = Registry.ClassesRoot.OpenSubKey(ShellCommandPath);
      if (commandKey != null)
      {
        var currentCommandValue = commandKey.GetValue(string.Empty, string.Empty) as string;
        if (!string.IsNullOrEmpty(currentCommandValue))
        {
          validValueIsPresent = currentCommandValue.Equals(shellCommandValue, StringComparison.OrdinalIgnoreCase);
        }
      }

      if (!validValueIsPresent)
      {
        CreateContextMenuRegistration(shellCommandValue);
      }
    }


    public static bool GetRegistrationIsSwitchedOn()
    {
      RegistryKey appKey = GetAppKey();
      if (!appKey.GetValueNames().Contains(ContextMenuRegistrationValueName))
      {
        return false;
      }

      if (appKey.GetValueKind(ContextMenuRegistrationValueName) != RegistryValueKind.DWord)
      {
        return false;
      }

      return ((int)appKey.GetValue(ContextMenuRegistrationValueName, 0)) == 1;
    }

    public static void SetRegistrationIsSwitchedOn(bool value)
    {
      RegistryKey appKey = GetAppKey();
      appKey.SetValue(ContextMenuRegistrationValueName, value ? 1 : 0, RegistryValueKind.DWord);
    }

    #endregion

    #region Methods

    private static void CreateContextMenuRegistration(string commandValue)
    {
      RegistryKey commandKey = Registry.ClassesRoot.CreateSubKey(ShellCommandPath);
      commandKey.SetValue(string.Empty, commandValue, RegistryValueKind.String);
    }

    private static RegistryKey GetAppKey()
    {
      return Registry.CurrentUser.CreateSubKey(ToolSubKeyPath);
    }

    private static string GetShellCommandValue(string pathToExe)
    {
      string trimmedPath = pathToExe.Trim();
      bool quoteDecorate = !trimmedPath.StartsWith("\"");

      string commandPathFormatStr = quoteDecorate ? "\"{0}\" \"%1\"" : "{0} \"%1\"";
      return string.Format(commandPathFormatStr, trimmedPath);
    }

    private static void RemoveContextMenuRegistration()
    {
      if (Registry.ClassesRoot.OpenSubKey(ShellApplicationPath) != null)
      {
        Registry.ClassesRoot.DeleteSubKeyTree(ShellApplicationPath);
      }
    }

    #endregion
  }
}