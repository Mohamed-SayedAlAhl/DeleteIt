# Delete It

## Description

**Delete It** is a powerful Windows Forms application designed to handle the deletion of stubborn files and folders that are typically in use or locked by the system. This application allows users to easily delete files, even those that are hard to remove, by providing intuitive tools and features for managing file access.

## Key Features

- **File Deletion Management:** 
  
  - Facilitate the deletion of files that are currently in use by allowing users to terminate processes, freeing the files for deletion upon user confirmation.

- **Scheduling Deletion:**
  
  - Implement a scheduling feature for files used by the Windows Explorer system, enhancing usability for files that cannot be deleted immediately.

- **Context Menu Integration:**
  
  - Add context menu options in Windows Explorer, enabling users to quickly delete multiple files and folders directly from the right-click menu, simplifying file management.

- **Process Management:**
  
  - Detect active applications utilizing files in a folder. If a user attempts to delete a folder with open applications, the app lists all active processes and their corresponding file paths, allowing users to close them easily.

- **Ownership and Permissions:**
  
  - Utilize take ownership functionality to grant necessary permissions, enabling deletion of protected files, such as those from the Windows Store, that typically require elevated access rights.

## Outcomes

- Enhanced user experience by providing a straightforward interface for managing file deletions, reducing frustration with traditional file deletion methods.
- Increased efficiency in file management tasks by allowing users to deal with open files directly and schedule deletions seamlessly.
- Successfully improved overall system performance by enabling users to remove unwanted files that are locked or in use, thus freeing up system resources.

## Installation

1. Clone the repository:
   
   ```bash
   git clone https://github.com/mohamed-sayedalahl/deleteit.git
   ```

2. Open the solution in Visual Studio.

3. Build the project to restore dependencies.

4. Run the application.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any enhancements or bug fixes.

## License

For more details, please refer to the [LICENSE](https://github.com/Mohamed-SayedAlAhl/deleteit/blob/main/LICENSE) file.
