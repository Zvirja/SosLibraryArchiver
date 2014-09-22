using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SosLibraryArchiver
{
  internal class Program
  {
    #region Public Methods and Operators

    public static bool? IsX86Lib(string pFilePath)
    {
      ushort architecture = 0;
      try
      {
        using (System.IO.FileStream fStream = new System.IO.FileStream(pFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
        {
          using (System.IO.BinaryReader bReader = new System.IO.BinaryReader(fStream))
          {
            if (bReader.ReadUInt16() == 23117) //check the MZ signature
            {
              fStream.Seek(0x3A, System.IO.SeekOrigin.Current); // seek to e_lfanew.
              fStream.Seek(bReader.ReadUInt32(), System.IO.SeekOrigin.Begin); //Seek to the start of the NT header.
              if (bReader.ReadUInt32() == 17744) // check the PE\0\0 signature.
              {
                fStream.Seek(20, System.IO.SeekOrigin.Current); // seek past the file header, and
                architecture = bReader.ReadUInt16(); // read the magic number of the optional header.
              }
            }
          }
        }
      }
      catch (Exception)
      {
        return null;
      }

      if (architecture == 0x10b)
      {
        return true;
      }

      if (architecture == 0x20b)
      {
        return false;
      }

      return null;
    }

    #endregion

    #region Methods

    private static bool HandleRegistrationParam(string[] args)
    {
      bool isRegChangeCommand = false;
      if (args[0].Equals("/regshell", StringComparison.OrdinalIgnoreCase))
      {
        RegistryHelper.SetRegistrationIsSwitchedOn(true);
        isRegChangeCommand = true;
      }
      else if (args[0].Equals("/unregshell", StringComparison.OrdinalIgnoreCase))
      {
        RegistryHelper.SetRegistrationIsSwitchedOn(false);
        isRegChangeCommand = true;
      }

      RegistryHelper.EnsureContextMenuRegistrationConsistent(Assembly.GetExecutingAssembly().Location);
      return isRegChangeCommand;
    }

    private static void Main(string[] args)
    {
      if (args.Length != 1)
      {
        PrintError("More than one arg should be passed.");
        return;
      }

      if (HandleRegistrationParam(args))
      {
        return;
      }

      string pathToDll = args[0];

      ProcessLibrary(pathToDll, new HashSet<string>());
    }

    private static void PrintError(string message)
    {
      Console.WriteLine(message);
      Console.ReadKey();
    }

    private static void ProcessLibrary(string pathToDll, HashSet<string> alreadyProcessed)
    {
      if (alreadyProcessed.Contains(pathToDll.ToLowerInvariant()))
      {
        PrintError("Stack overflow prevented. Unable to relocate assembly.");
        return;
      }

      alreadyProcessed.Add(pathToDll.ToLowerInvariant());

      string originalName = Path.GetFileNameWithoutExtension(pathToDll);
      if (originalName == null)
      {
        PrintError("Cannot get name without extension.");
        return;
      }

      string parentDir = Path.GetDirectoryName(pathToDll);
      if (parentDir == null)
      {
        PrintError("Cannot resolve parent dir.");
        return;
      }

      string newName;

      int indexOfDot = originalName.IndexOf('.');
      if (indexOfDot < 0)
      {
        bool? isX86 = IsX86Lib(pathToDll);
        FileVersionInfo assemblyVersion = FileVersionInfo.GetVersionInfo(pathToDll);
        var assemblyVersionString = string.Join(".",
          new string[]
          {
            assemblyVersion.FileMajorPart.ToString(),
            assemblyVersion.FileMinorPart.ToString(),
            assemblyVersion.FileBuildPart.ToString(),
            assemblyVersion.FilePrivatePart.ToString()
          });

        string bitnessString = isX86 != null ? (isX86.Value ? "X86" : "X64") : "UNK";


        newName = originalName + "." + bitnessString + "." + assemblyVersionString;
      }
      else
      {
        newName = originalName.Substring(0, indexOfDot);
      }

      string newLoc = Path.Combine(parentDir, newName + Path.GetExtension(pathToDll));

      if (File.Exists(newLoc))
      {
        ProcessLibrary(newLoc, alreadyProcessed);
      }

      if (File.Exists(newLoc))
      {
        PrintError("Cannot find place for library.");
        return;
      }

      File.Move(pathToDll, newLoc);
    }

    #endregion
  }
}