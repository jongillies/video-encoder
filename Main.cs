using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoEncoder
{
	/// <summary>
	/// Summary description for MainClass.
	/// </summary>
	/// 
	class MainClass
	{
		[STAThread]
		static void Main(string[] args)
		{
			StreamWriter stdOut = new StreamWriter(Console.OpenStandardOutput());
			if (args.Length == 0)
			{
				stdOut.WriteLine ("No DVD path specified on the command line.");
				stdOut.Close();
				return;
			}

			string destination = args[1];
			destination = Path.ChangeExtension(destination, null);

			Encode encoder = new Encode();
			encoder.source = args[0];
			encoder.destination = destination;
			if (args.Length > 2)
			{
				encoder.profile = args[2];
			}
			encoder.Run();
		}
	}
}
