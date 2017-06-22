using CommandLine;
using SInnovations.VSTeamServices.TaskBuilder.Attributes;
using SInnovations.VSTeamServices.TaskBuilder.ConsoleUtils;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Echo Task")]
[assembly: AssemblyDescription("Write out message")]
[assembly: AssemblyInformationalVersion("1.0.6")]  //Update to do new release
[assembly: AssemblyConfiguration("Utility")]
[assembly: AssemblyCompany("S-Innovations v/Poul K. Sørensen")]
[assembly: AssemblyProduct("EchoTask")]
[assembly: AssemblyTrademark("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("df3b53b1-b669-4bde-b0d1-162b982255a7")]

namespace EchoTask
{

    [EntryPoint("Sending sms to $(ReceiverNumber)")]
    public class ProgramOptions
    {
     
        [Option("Message")]
        public string Message { get; set; }
        
    }

    class Program
    {
        static void Main(string[] args)
        {
            var ops = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Sending SMS", args);

            Console.WriteLine("Echo 2 : " + ops.Message);

        }
    }
}
