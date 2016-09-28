## DotNetDevOps.VSTeamServices

### Getting Started
Clone the respostiory and open in visual studio, build the tasks.
Run the generated task `AzureBlobFileCopy.exe --build` and upload using `tfx build tasks upload --task-path ./` from the bin output folder

The easiest way to get stated is to open the solution, add a new dotnet core commandlin tool and copy over project.json from one of the others to resolve dependencies and use the net46 framework. (I am working on relaxing the requirement of net46 that currently exists.)

#### EntryPoint
The EntryPoint attribute allows you to mark a CLR type that holds the input arguments
```
    [EntryPoint("Creating Build Artifact")]
    public class ProgramOptions
    {  
        [Option("OutPutFileName")]
        public string OutPutFileName { get; set; }

    }
```
In the above example the Option attribute is from the CommandLine Parser library used and the minimum required attribute to create an input.

#### Parsing arguments
Using the commandline helper to parse arguments allows the use of ect `--build` to trigger the generating of helper files and task.json. 
Currently it uses a powershell runner to wrap the commandline tool when running the task in visual studio team services. A future extension would be to generate a node script and allow cross platform tasks to be build also using dotnet core.

```
	static void Main(string[] args)
    {
		var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Running Create AzureAD Applcaition Task", args);

	}
```

Examples in the repository currenly have more advanced examples and building them and running with --build allows you to see the task.json for each of them.

#### Examples

Use the DisplayAttribute to create description and a friendly Display name for the input generated.
```
	[Display(Description = "The path to save the artifact file", Name = "Output File Name")]
    [Option("OutPutFileName")]
    public string OutPutFileName { get; set; }
```

Use the DisplayAttribute to set a ResourceType of GlobPath which will allow the `ConsoleHelper.ParseAndHandleArguments` to find files using GlobPaths, and also make the UI generate the filepicker dialog on visual studio team services.
```
	[Display(ShortName = "JsonFile", Name = "Json File", Description = "Path to the json file to update", ResourceType = typeof(GlobPath))]
	public GlobPath JsonFiles { get; set; }
```
together with 
```
        var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Editing Json File", args);

        foreach (var file in options.JsonFiles.MatchedFiles())
        {
		}			
```
or using 
```
```
	[Display(ShortName = "JsonFile", Name = "Json File", Description = "Path to the json file to update", ResourceType = typeof(GlobPath))]
	public string JsonFiles { get; set; }
```
to make it use the filepicker and resolve to one file.

The following assembly attributes must be defined for the generator to work currently.
```
[assembly: AssemblyInformationalVersion("1.0.1")]
[assembly: AssemblyTitle("Update a Json File using Json Path")]
[assembly: AssemblyDescription("Using JsonPath properties can be replaced in json files")]
[assembly: AssemblyConfiguration("Utility")]
[assembly: AssemblyCompany("S-Innovations.Net v/Poul K. Sørensen")]
[assembly: AssemblyProduct("JsonPathManipulationTask")]
[assembly: Guid("d8a3ded4-c05c-42ed-af36-b98a891304b6")] //DONT COPY THIS, its unique for each task.
```



