
# Bxl Sharp
BxlSharp is a .NET Library designed to facilitate the parsing and reading of UltraLibrarian files within .NET projects. UltraLibrarian is a file format commonly used in the electronic design automation industry for the storage and exchange of component information and library data.

This library enables the integration of UltraLibrarian file reading functionality into .NET applications, making it easy to work with component libraries or extract data from UltraLibrarian files. In particular, the library allows for the efficient handling of BXL files, which contain information on components and their associated footprints.

# Installation
Installation of the library is straightforward, as it is available via NuGet.

To add the OriginalCircuit.BxlSharp library to a .NET project using the Package Manager Console, follow these steps:

1.  Open the Package Manager Console window in Visual Studio. You can do this by going to `Tools > NuGet Package Manager > Package Manager Console`.
2.  In the Package Manager Console window, enter the following command to install the OriginalCircuit.BxlSharp library:

    `Install-Package OriginalCircuit.BxlSharp` 

3.  Press Enter to execute the command. The OriginalCircuit.BxlSharp library will be downloaded and added to your project.

# Usage
To use the Ultralibrarian Reader .NET Library in your .NET project, you will need to include the following using statements at the top of your source file:

```C#
    using OriginalCircuit.BxlSharp;
    using OriginalCircuit.BxlSharp.Types;
```

These using statements bring the classes and types from the `OriginalCircuit.BxlSharp` and `OriginalCircuit.BxlSharp.Types` namespaces into scope, allowing you to use them in your code without having to specify the full namespace every time.

Once you have included these using statements, you can use the classes and types from the `OriginalCircuit.BxlSharp` and `OriginalCircuit.BxlSharp.Types` namespaces in your code. For example, you can use the `BxlDocument` class to open and read UltraLibrarian files, and the `LibPin` class to access information about the pins in a symbol.

## Opening BXL Files
Here's an example of how to open an UltraLibrarian file:

```C#
    // Open the Ultralibrarian file  
    var data = await  BxlDocument.ReadFromFileAsync(fileName, BxlFileType.FromExtension);
```

This code opens the UltraLibrarian file with the specified `fileName` and returns the data as a `BxlDocument` object. The `BxlFileType.FromExtension` parameter tells the library to determine the file type based on the file extension.

## Reading Pin Information From All Symbols
To read the pin information for each symbol in the UltraLibrarian file, you can use the following code:

```C#
    // Loop through each symbol in the file 
    foreach (var  symbol  in data.Symbols) 
    { 
        // Get the pin information for the symbol  
        var pins = symbol.Data .Where(d => d is LibPin &&
            (d as  LibPin).Name.Text.ToUpperInvariant() != "NC" && 
            (d as  LibPin).Name.Text.ToUpperInvariant() != "N/C" && 
            (d as  LibPin).Name.Text.ToUpperInvariant() != "DNC")
         .Select(d => d as  LibPin).ToList(); 
     
        //further process the pin list
     }
 ```

In this example, the `Pin` class is a custom class that is defined in the user's code and has a `Designator` and `Name` property. The `LibPin` objects are converted to `Pin` objects by creating a new `Pin` object and passing in the `Designator` and `Name` values from the `LibPin` object. The resulting `Pin` objects are then added to a `List<Pin>` object.

You can then use the `pinList` object to access the pin information for each symbol in the UltraLibrarian file.
