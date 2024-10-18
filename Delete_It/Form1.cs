using Microsoft.Win32;
using System;
using System.Collections.Generic;

using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using System.Windows.Forms;



namespace Delete_It
{
    public partial class Form1 : Form
    {
        

        public Form1()
        {
			this.StartPosition = FormStartPosition.CenterScreen;
			InitializeComponent();

			


			button1.BackColor = Color.Transparent;
			button1.FlatStyle = FlatStyle.Flat;

			// Remove the button's border
			button1.FlatAppearance.BorderSize = 0;


			btnDeleteFiles.BackColor = Color.Transparent;

			
			btnDeleteFiles.FlatStyle = FlatStyle.Flat;
			btnDeleteFiles.FlatAppearance.BorderSize = 0;
			



			CheckDir_deleteFolderPath();
			AddToRegistry();


			

			// Check for command-line arguments on form load (silent mode)
			string[] args = Environment.GetCommandLineArgs();

			if (args.Length > 1)
			{


                // Silent mode: process files or folders
                HandleSilentMode(args);

				
				// Exit the application immediately after processing
				
				Environment.Exit(0);

            }

		}

		private static List<bool> sheduledforRebootList = new List<bool>();
		
		
		private static bool sheduledforReboot =false;
		

		private static int numberOfSuccessMessages =0;
        private static string DeletedPaths ="";

		
		private static string deleteFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".delete it");
		private static string Reg_txt = Path.Combine(deleteFolderPath, "Deleteit_appLocation.txt");
		private static string scheduleFilePath = Path.Combine(deleteFolderPath, "Deleteit_rebootScheduled.txt");
       
		private static string AppName = "";

		

		private static void CheckDir_deleteFolderPath()
		{
			// Ensure the directory exists
			if (!Directory.Exists(deleteFolderPath))
			{
				Directory.CreateDirectory(deleteFolderPath);
			}

		}

		private static void AddVbsToStartup()
		{
			
			string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
			string vbsFilePath = Path.Combine(startupFolderPath, "DeleteSchedule.vbs");

			// Create the VBS script file with Chr(34) for quoting paths
			string vbsContent = "Dim fso, filePath\n" +
								"Set fso = CreateObject(\"Scripting.FileSystemObject\")\n" +
								$"filePath = \"{scheduleFilePath}\"\n" + // Use the actual path here
								"If fso.FileExists(filePath) Then\n" +
								"    fso.DeleteFile filePath\n" +
								"End If\n" +
								"Set oShell = CreateObject(\"WScript.Shell\")\n" +
								$"oShell.Run \"cmd /c del \" & Chr(34) & WScript.ScriptFullName & Chr(34), 0, False";

			// Write the VBS content to the file
			File.WriteAllText(vbsFilePath, vbsContent);
		}


		private static bool IsDirectorySceduledBefore(string CurrentPath)
        {
			// Check if the scheduled file exists before trying to read it
			if (!File.Exists(scheduleFilePath))
			{
				File.Create(scheduleFilePath).Close(); // Create the file if it doesn't exist
			}

			// Read the contents of the scheduled file
			var scheduledPaths = File.ReadAllLines(scheduleFilePath).ToList();

			string directoryToCheck = Directory.Exists(CurrentPath) ? CurrentPath : Path.GetDirectoryName(CurrentPath);

			return directoryToCheck != null && scheduledPaths.Contains(directoryToCheck);


		}
		private static void HandleSilentMode(string[] args)
		{
			// Start from the second argument (index 1) since the first one is the executable path
			for (int i = 1; i < args.Length; i++)
			{
				string path = args[i];

				if (Directory.Exists(path))
				{
					DeleteFolder(path);
				}
				else if (File.Exists(path))
				{
					DeleteFile(path);
				}


				//// Check if any path in sheduledforRebootList is not already in the scheduled paths
				string directoryToCheck = Directory.Exists(path) ? path : Path.GetDirectoryName(path);

				bool needsReschedule = sheduledforRebootList.Any() && !IsDirectorySceduledBefore(path);




				if (needsReschedule)
				{
					// Write the directory to the file since it's not already present
					File.AppendAllText(scheduleFilePath, directoryToCheck + Environment.NewLine);

					// Schedule a reboot if any path isn't already scheduled
					AddVbsToStartup();
					ScheduleReboot();
				}
			}
		}


		public static void AddToRegistry()
        {
            string exePath = Application.ExecutablePath;

            // Check if the registry file exists and read the existing path
            string existingExePath = string.Empty;
            if (File.Exists(Reg_txt))
            {
                existingExePath = File.ReadAllText(Reg_txt);
            }

            // Check if the executable path has changed
            if (existingExePath == exePath)
            {
                return; // No change, so exit
            }

            // Update the text file with the new executable path
            File.WriteAllText(Reg_txt, exePath);

            // Create a combined context menu entry for deleting files and folders
            using (RegistryKey deleteKey = Registry.ClassesRoot.OpenSubKey(@"*\shell\Delete Selected", true)
                ?? Registry.ClassesRoot.CreateSubKey(@"*\shell\Delete Selected"))
            {
                if (deleteKey != null)
                {
                    deleteKey.SetValue("Icon", exePath);
                    deleteKey.CreateSubKey("command").SetValue("", $"\"{exePath}\" \"%V\"");
                }
            }

            // Make sure to handle the folder context as well
            using (RegistryKey folderKey = Registry.ClassesRoot.OpenSubKey(@"Directory\shell\Delete Selected", true)
                ?? Registry.ClassesRoot.CreateSubKey(@"Directory\shell\Delete Selected"))
            {
                if (folderKey != null)
                {
                    folderKey.SetValue("Icon", exePath);
                    folderKey.CreateSubKey("command").SetValue("", $"\"{exePath}\" \"%V\"");
                }
            }

            // Set MultipleInvokePromptMinimum to the maximum 32-bit integer value (2147483647)
            using (RegistryKey explorerKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true))
            {
                if (explorerKey != null)
                {
                    explorerKey.SetValue("MultipleInvokePromptMinimum", 214748364, RegistryValueKind.DWord);
                }
            }


        }


        private static void ShowDeletionMessage()
		{
			
			CustomMessageBox.Show($"Deleted Successfully :\n {DeletedPaths} ", "Deletion Info", MessageBoxIcon.Information,true,"OK");

		}

		private static void ConstructDeletionMessage(string path)
		{


			DeletedPaths += $"{path}\n";
			numberOfSuccessMessages++;

		}

		public static void HandleAccessDeniedErrorForFolders(string directoryPath)
		{
			if (IsDirectorySceduledBefore(directoryPath))
            {
				CustomMessageBox.Show("This folder is already scheduled for deletion upon reboot", "Reboot Scheduled", MessageBoxIcon.Information, true, "OK");
				return;
			}


			sheduledforReboot = true;
			AddtoRebootList(true);

			try
			{

				
				// Rename all files in the directory
				RenameFilesInDirectory(directoryPath);
				// Rename all subdirectories in the directory
				RenameDirectoriesInDirectory(directoryPath);

				
				
			}
			catch (Exception)
			{
				//Renaming Failed

			}

			finally
            {
				// Clean the directory
				CleanDirectoryRecursively(directoryPath);

			}
			



		}
		public static void HandleAccessDeniedErrorForFiles(string FilePath)
		{

			if (IsDirectorySceduledBefore(FilePath))
            {
				CustomMessageBox.Show("This file is already scheduled for deletion upon reboot", "Reboot Scheduled", MessageBoxIcon.Information, true, "OK");

				return;
			}

			sheduledforReboot = true;
			AddtoRebootList(true);

			try
			{
				

				// Rename all files in the directory
				RenameFile(FilePath);

			}

			catch (Exception)
			{

				ScheduleFolderDeletion(FilePath);

			}

			
		}
		[Flags]
        internal  enum MoveFileFlags
        {
            None = 0,
            ReplaceExisting = 1,
            CopyAllowed = 2,
            DelayUntilReboot = 4,
            WriteThrough = 8,
            CreateHardlink = 16,
            FailIfNotTrackable = 32,
        }

        internal static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool MoveFileEx(
                string lpExistingFileName,
                string lpNewFileName,
                MoveFileFlags dwFlags);
        }
        public static void ScheduleFolderDeletion(string folderPath)
        {

            folderPath = folderPath.Replace("\\", "\\\\");



            // Schedule deletion using MoveFileEx without quoting the path
            if (!NativeMethods.MoveFileEx(folderPath, null, MoveFileFlags.DelayUntilReboot))
            {
                int errorCode = Marshal.GetLastWin32Error();
                CustomMessageBox.Show($"Failed to schedule deletion: {errorCode} - {new System.ComponentModel.Win32Exception(errorCode).Message}",
                                 "Error", MessageBoxIcon.Error,true,"OK");
            }

        }

        private static void CleanDirectoryRecursively(string directoryPath)
        {
            // Process all files in the current directory
            foreach (var filePath in Directory.GetFiles(directoryPath))
            {

                ScheduleFolderDeletion(filePath);
            }

            // Recursively process subdirectories
            foreach (var subDirectoryPath in Directory.GetDirectories(directoryPath))
            {
				

				CleanDirectoryRecursively(subDirectoryPath);
            }

            // Schedule the current directory for deletion on reboot
            ScheduleFolderDeletion(directoryPath);
        }

        private static void TakeOwnership(string path)
        {


            try
            {
                ProcessStartInfo takeOwnInfo = new ProcessStartInfo("cmd.exe", $"/c takeown /f \"{path}\" /r /d y")
                {
                    Verb = "runas",  // Run as administrator
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process takeOwnProcess = Process.Start(takeOwnInfo);
                takeOwnProcess?.WaitForExit();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error taking ownership: {ex.Message}", "Getting Full Access Error", MessageBoxIcon.Error,true,"OK" );
            }
        }

        private static void GrantFullControlAndRemoveSystem(string path)
        {
            try
            {
                // Grant full control to the current user
                ProcessStartInfo grantInfo = new ProcessStartInfo("cmd.exe", $"/c icacls \"{path}\" /grant \"%USERNAME%:F\" /t")
                {
                    Verb = "runas",  // Run as administrator
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process grantProcess = Process.Start(grantInfo);
                grantProcess?.WaitForExit();

                // Remove all other permissions, including SYSTEM and TrustedInstaller
                ProcessStartInfo removeInfo = new ProcessStartInfo("cmd.exe", $"/c icacls \"{path}\" /remove:g SYSTEM TrustedInstaller /t")
                {
                    Verb = "runas",  // Run as administrator
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process removeProcess = Process.Start(removeInfo);
                removeProcess?.WaitForExit();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error removing system permissions: {ex.Message}", "Permissions Are Required", MessageBoxIcon.Error,true,"OK");
            }
        }


		private static void DeleteFolder(string directoryPath)
        {

			string folderName = Path.GetFileName(directoryPath);

		aa:
            try
            {
				sheduledforReboot = false;
				

				try
				{
					Directory.Delete(directoryPath, true);

					
				}

				catch (Exception)
				{

					TakeOwnership(directoryPath);
					GrantFullControlAndRemoveSystem(directoryPath);
					// Attempt to delete the folder and its contents
					Directory.Delete(directoryPath, true);
				}


			}
			catch (UnauthorizedAccessException)
            {

                // Call HandleAccessDeniedError if access is denied
                HandleAccessDeniedErrorForFolders(directoryPath);
            }
            catch (IOException ioEx) when (ioEx.HResult == -2147024864) // HResult for "The process cannot access the file because it is being used by another process"
            {
				DialogResult result;
				


				ProcessKiller.condition conditon = ProcessKiller.CloseProcessUsingFolder(directoryPath);
				
				if (conditon==ProcessKiller.condition.Accepted)
                {
					
					goto aa;
                }

				else 
				{

				
					string message = "";

					 message = ProcessKiller.GetCurrentActiveAppsInFolder(directoryPath);

                    if (message!="")
                    {
						result = CustomMessageBox.Show($"Please close these apps: {message}\nand then click 'Done' to continue with the deletion.\nIf you're unable to close them, a reboot will be required.",
													"Folder is In Use",
													MessageBoxIcon.Warning, false, "Done", "Couldn't");

						if (result == DialogResult.Yes)
						{
							goto aa;
						}
						else
						{
							HandleAccessDeniedErrorForFolders(directoryPath);
						}


					}

                    else
                    {
						//No Active Apps
						if (ProcessKiller.ActiveApps.Count == 0)
							goto aa;

						result = CustomMessageBox.Show($"Please close the folder or any files/subfolders inside it that are currently in use so we can proceed with deletion.\nOnce closed, click 'Done' to continue.\nIf you're unable to close them, a reboot will be required.",
												"Folder is In Use",
												MessageBoxIcon.Warning, false, "Done", "Couldn't");




						if (result == DialogResult.Yes)
						{
							goto aa;
						}
						else
						{
							HandleAccessDeniedErrorForFolders(directoryPath);
						}
					}


					


				}

              



			}
            catch (Exception ex)
            {
                CustomMessageBox.Show($"An error occurred while deleting the folder: {ex.Message}","Error",MessageBoxIcon.Error,true,"OK");
            }

        }

		private static void DeleteFile(string FilePath)
		{
			
			string fileName = Path.GetFileNameWithoutExtension(FilePath);
			

		aa:
			try
			{

				sheduledforReboot = false;
				

				try
                {

					File.Delete(FilePath);
					
				}
                catch (Exception)
                {
					TakeOwnership(FilePath);
					GrantFullControlAndRemoveSystem(FilePath);
					// Attempt to delete the folder and its contents
					File.Delete(FilePath);
				}
				

				
				

			}
			catch (UnauthorizedAccessException)
			{
				// Call HandleAccessDeniedError if access is denied
				HandleAccessDeniedErrorForFiles(FilePath);
			}
			catch (IOException ioEx) when (ioEx.HResult == -2147024864) // HResult for "The process cannot access the file because it is being used by another process"
			{
				
				DialogResult result;
				ProcessKiller.condition conditon = ProcessKiller.condition.ProcessNotFound;
				conditon = ProcessKiller.CloseProcessUsingFile(FilePath);

				if (conditon == ProcessKiller.condition.Accepted)
                {
					
					goto aa;
				}


				else 
				{
					ProcessKiller.condition Currentconditon = ProcessKiller.checkCurrentProcess(FilePath);
					if (Currentconditon == ProcessKiller.condition.ProcessNotFound)
						goto aa;

					AppName = ProcessKiller.GetActiveAppNamesInUse();

					string message = AppName != "" ? $": '{AppName}' as it uses the file" : " that uses the file";
					
						result = CustomMessageBox.Show($"Please close the app{message}: {fileName} and preventing us from deleting the file. Once closed, click 'Done' to continue.\nIf you're unable to close it, a reboot will be required.",
													"File is In Use",
													MessageBoxIcon.Warning, false, "Done", "Couldn't");


						if (result == DialogResult.Yes)
						{
							goto aa;
						}
						else
						{
							HandleAccessDeniedErrorForFiles(FilePath);
						}
					

					
				}


				
			}
			catch (Exception ex)
			{
				CustomMessageBox.Show($"An error occurred while deleting the File: {ex.Message}","Error",MessageBoxIcon.Error,true,"OK");
			}
		}
		private static void ScheduleReboot()
		{
			
			if (CustomMessageBox.Show("A reboot is required. Would you like to reboot now?", "Reboot Required", MessageBoxIcon.Information,false,"Reboot Now","Reboot Later",false) == DialogResult.Yes)
			{

				Process.Start("shutdown", "/r /t 0"); // Reboot the system immediately
			}
			


		}

		private static void RenameFile(string filePath)
		{
			string hiddenChar = "\u200B";

			// Get the directory of the original file
			string directoryPath = Path.GetDirectoryName(filePath);

			// Create the new file path with the hidden character inserted before the extension
			string newFilePath = Path.Combine(directoryPath, $"{Path.GetFileNameWithoutExtension(filePath)}{hiddenChar}{Path.GetExtension(filePath)}");

			// Rename the file
			File.Move(filePath, newFilePath);

			ScheduleFolderDeletion(newFilePath);
		}


		private static void RenameFilesInDirectory(string directoryPath)
        {
            string hiddenChar = "\u200B";

            foreach (var filePath in Directory.GetFiles(directoryPath))
            {
                string newFilePath = Path.Combine(directoryPath, $"{Path.GetFileNameWithoutExtension(filePath)}{hiddenChar}{Path.GetExtension(filePath)}");
                File.Move(filePath, newFilePath); // Rename the file
            }
        }

     
        private static void RenameDirectoriesInDirectory(string directoryPath)
        {

			

			string hiddenChar = "\u200B";
            foreach (var dirPath in Directory.GetDirectories(directoryPath))
            {
                string newDirPath = Path.Combine(Path.GetDirectoryName(dirPath), $"{Path.GetFileName(dirPath)}{hiddenChar}");
                Directory.Move(dirPath, newDirPath); // Rename the directory
            }
        }


		public static void AddtoRebootList(bool newBool)
		{
			// Add the new boolean to the list
			sheduledforRebootList.Add(newBool);
		}

		

		private void button1_Click(object sender, EventArgs e)
        {
			VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
			IntPtr ownerHandle = this.Handle;

			dialog.Multiselect = true;

			numberOfSuccessMessages = 0;
			DeletedPaths = "";


			bool? Result = dialog.ShowDialog(ownerHandle);

			if (Result == true)
			{
				int numberofPaths = dialog.SelectedPaths.Length;
				int i = 1;
				string toBeDeletedFolders = "";
				foreach (string SelectedPath in dialog.SelectedPaths)
				{

					toBeDeletedFolders += "\n" + SelectedPath;

				}

				if (CustomMessageBox.Show($"Are you sure you want to delete these folders:\n {toBeDeletedFolders}", "Folder Deletion Request", MessageBoxIcon.Warning) == DialogResult.No)
				{
					return;
				}

				foreach (string SelectedPath in dialog.SelectedPaths)
				{
					

					DeleteFolder(SelectedPath);

					if (!sheduledforReboot)
					{
						ConstructDeletionMessage(SelectedPath);

					}

					if (i == numberofPaths )
					{
						if (numberOfSuccessMessages > 0)
							ShowDeletionMessage();

						if (sheduledforRebootList.Any())
						{
							ScheduleReboot();
						}
					}
					i++;

				}

			}
		}

        private void btnDeleteFiles_Click(object sender, EventArgs e)
        {

			
			openFileDialog1.Title = "Select Files";
			
			openFileDialog1.Multiselect = true;
			openFileDialog1.FileName = "";


			numberOfSuccessMessages = 0;
			DeletedPaths = "";

			if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
				int numberofPaths = openFileDialog1.FileNames.Length;
				int i = 1;
				string toBeDeletedFiles = "";
				foreach (string SelectedPath in openFileDialog1.FileNames)
                {
					
					toBeDeletedFiles += "\n" + SelectedPath;

                }

				if (CustomMessageBox.Show($"Are you sure you want to delete these files:\n {toBeDeletedFiles}", "File Deletion Request", MessageBoxIcon.Warning) == DialogResult.No)
				{
					return;
				}

				foreach (string SelectedPath in openFileDialog1.FileNames)
				{
					

					DeleteFile(SelectedPath);
					if (!sheduledforReboot)
                    {

						ConstructDeletionMessage(SelectedPath);

					}
					if (i == numberofPaths)
					{
						if (numberOfSuccessMessages > 0)
							ShowDeletionMessage();

						if (sheduledforRebootList.Any())
						{
							ScheduleReboot();
						}
					}
					i++;

				}

			}
		}


	}

	
}
