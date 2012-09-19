using System;
using System.Collections;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace VideoEncoder
{
	/// <summary>
	/// Summary description for Encode.
	/// </summary>
	public class Encode
	{
		StreamWriter stdOut = null;
		private string _source = null;
		private string _destination = null;
		private string _configFile = null;
		private string _profile = null;
		private string _cropCommand = null;

		public string source 
		{
			get { return _source; }
			set { _source = value; }
		}

		public string destination 
		{
			get { return _destination; }
			set { _destination = value; }
		}

		public string configFile 
		{
			get { return _configFile; }
			set { _configFile = value; }
		}

		public string profile 
		{
			get { return _profile; }
			set { _profile = value; }
		}

		static bool DestinationExists(string destination)
		{
			string[] files = Directory.GetFiles(Path.GetDirectoryName(destination), Path.GetFileNameWithoutExtension(destination) + ".*");
			if (files.Length > 0)
			{
				return true;
			}
			return false;
		}

		static void WriteRiff(string destination, string fourCC)
		{
			FileStream fs = new FileStream(destination, FileMode.Open, FileAccess.ReadWrite);
			byte[] fileData = new byte[256];
			fs.Read(fileData, 0, 256);
			ASCIIEncoding enc = new ASCIIEncoding();
			string riffTag = enc.GetString(fileData, 0, 4);
			//int fileLen = BitConverter.ToInt32(fileData, 4);
			string fileType = enc.GetString(fileData, 8, 3);
			string mainListTag = enc.GetString(fileData, 12, 4);
			//int mainListSize = BitConverter.ToInt32(fileData, 16);
			string mainListType = enc.GetString(fileData, 20, 8);
			if ((string.Compare(riffTag, "RIFF") != 0) || (string.Compare(fileType, "AVI") != 0) || 
				(string.Compare(mainListTag, "LIST") != 0) || (string.Compare(mainListType, "hdrlavih") != 0))
			{
				// File is not a RIFF file
				fs.Close();
				return;
			}
			// Check the AVI type
			string aviHeaderTag = enc.GetString(fileData, 100, 4);
			string aviTag = enc.GetString(fileData, 108, 4);
			if ((string.Compare(aviHeaderTag, "strh") != 0) || (string.Compare(aviTag, "vids") != 0))
			{
				// File is not an AVI video
				fs.Close();
				return;
			}
			string aviHandlerCode = enc.GetString(fileData, 112, 4);
			// Write the handler code
			enc.GetBytes(fourCC, 0, 4, fileData, 112);
			// Check AVI format
			string aviFormatTag = enc.GetString(fileData, 164, 4);
			if (string.Compare(aviFormatTag, "strf") != 0)
			{
				// File is not an AVI stream
				fs.Close();
				return;
			}
			string aviFormatCode = enc.GetString(fileData, 188, 4);
			// Write the format code
			enc.GetBytes(fourCC, 0, 4, fileData, 188);

			fs.Seek(0, SeekOrigin.Begin);
			fs.Write(fileData, 0, 256);
			fs.Close();
		}

		static void FixRiff(string destination, string fourCC)
		{
			string[] files = Directory.GetFiles(Path.GetDirectoryName(destination), Path.GetFileNameWithoutExtension(destination) + ".*");
			foreach (string currentFile in files)
			{
				try
				{
					WriteRiff(currentFile, fourCC);
				} 
				catch {}
			}
		}

		public Encode()
		{
			stdOut = new StreamWriter(Console.OpenStandardOutput());
			stdOut.AutoFlush = true;
			_configFile = "VideoEncoder.config";
			_profile = "default";
		} // constructor

		private bool CropDetect (string encodeCommand, string encodeArguments)
		{
			DateTime start = DateTime.Now;
			bool result = false;

			Process newProc = new Process();
			newProc.StartInfo.FileName =encodeCommand;
			newProc.StartInfo.Arguments = encodeArguments;
			newProc.StartInfo.UseShellExecute = false;
			newProc.StartInfo.CreateNoWindow = true;
			newProc.StartInfo.RedirectStandardError = true;
			newProc.StartInfo.RedirectStandardInput = false;
			newProc.StartInfo.RedirectStandardOutput = true;

			try
			{
				newProc.Start();
			} 
			catch (Exception ex)
			{
				stdOut.Write("Error running crop detection: ");
				stdOut.WriteLine (ex.Message);
				return false;
			}

			stdOut.WriteLine ("Determining crop...");
			stdOut.WriteLine ("Start: " + start.ToString());

			StreamReader sOut = newProc.StandardOutput;
			StreamReader sErr = newProc.StandardError;

			string lastCropResult = null;
			string outputData = null;
			SortedList cropOccurance = new SortedList();
			int currentCrop = 0;
			while ((outputData = sOut.ReadLine()) != null)
			{
				sErr.BaseStream.Flush();
				if (outputData.StartsWith("crop area:") == true)
				{
					currentCrop = 0;
					try
					{
						currentCrop = (int) cropOccurance[outputData];
					} 
					catch {}
					
					cropOccurance[outputData] = ++currentCrop;

					//lastCropResult = outputData;
				} 
			}

			currentCrop = 0;
			foreach (object currentKey in cropOccurance.Keys)
			{
				if (currentCrop < (int)cropOccurance[currentKey])
				{
					lastCropResult = currentKey.ToString();
					currentCrop = (int)cropOccurance[currentKey];
				}
			}

			string cropCommand = null;
			if (lastCropResult != null)
			{
				Regex r = new Regex(@"(crop=\d+:\d+:\d+:\d+)");
				Match m = r.Match(lastCropResult);
				if (m.Success == true)
				{
					cropCommand = m.Value;
				}
			}

			if (cropCommand!= null)
			{
				_cropCommand = cropCommand;
				stdOut.WriteLine(cropCommand);
				result  = true;
			} 
			else 
			{
				stdOut.WriteLine("Unable to determine crop settings.");
				result = false;
			}

			stdOut.WriteLine ("End: " + DateTime.Now.ToString());
			TimeSpan lapse = DateTime.Now - start;
			stdOut.WriteLine ("Total time: " + lapse.ToString());

			sOut.Close();
			sErr.Close();
			newProc.Close();

			return result;
		}

		private bool EncodeProcess (string encodeCommand, string encodeArguments)
		{
			DateTime start = DateTime.Now;

			Process newProc = new Process();
			newProc.StartInfo.FileName =encodeCommand;
			newProc.StartInfo.Arguments = encodeArguments;
			newProc.StartInfo.UseShellExecute = false;
			newProc.StartInfo.CreateNoWindow = true;
			//newProc.StartInfo.RedirectStandardError = true;
			newProc.StartInfo.RedirectStandardError = false;
			newProc.StartInfo.RedirectStandardInput = false;
			newProc.StartInfo.RedirectStandardOutput = true;

			try
			{
				newProc.Start();
			} 
			catch (Exception ex)
			{
				stdOut.Write("Error running encode pass");
				stdOut.WriteLine (ex.Message);
				return false;
			}

			stdOut.WriteLine ("Encoding...");
			stdOut.WriteLine ("Start: " + start.ToString());

			StreamReader sOut = newProc.StandardOutput;
			//StreamReader sErr = newProc.StandardError;

			string outputData = null;
			string lastPercent = null;
			while ((outputData = sOut.ReadLine()) != null)
			{
				/*
				if (sErr.BaseStream.Length > 0)
				{
					string errors = sErr.ReadToEnd();
				}
				*/
				if (outputData.StartsWith("Pos:") == true)
				{
					bool writing = false;
					//stdOut.WriteLine(outputData);
					Regex r = new Regex(@"(\(.*%\))");
					Match m = r.Match(outputData);
					if (m.Success == true)
					{
						if (string.Compare(lastPercent, m.Value, true) != 0)
						{
							lastPercent = m.Value;
							stdOut.Write(lastPercent);
							writing = true;
						}
					}
					if (writing == true)
					{
						r = new Regex(@"(\d*min)");
						m = r.Match(outputData);
						if (m.Success == true)
						{
							stdOut.Write(" ");
							stdOut.Write(m.Value);
						}
						stdOut.Write("          \r");
					} // if writing
				}  // if
			} // while

			stdOut.WriteLine();
			stdOut.WriteLine ("End: " + DateTime.Now.ToString());
			TimeSpan lapse = DateTime.Now - start;
			stdOut.WriteLine ("Total time: " + lapse.ToString());

			sOut.Close();
			//sErr.Close();
			newProc.Close();

			return true;
		}

		private bool CleanupProcess (string encodeCommand)
		{
			DateTime start = DateTime.Now;

			Process newProc = null;

			try
			{
				newProc = Process.Start(encodeCommand);
			} 
			catch (Exception ex)
			{
				stdOut.Write("Error running cleanup. ");
				stdOut.WriteLine (ex.Message);
				return false;
			}

			stdOut.WriteLine ("Cleaning...");

			newProc.Close();

			return true;
		}

		public void Run()
		{
			VideoEncoderConfig currentConfig = null;
			try
			{
				currentConfig = VideoEncoderConfig.Load(_configFile);
			} 
			catch (Exception ex)
			{
				stdOut.Write("Error reading config file. ");
				stdOut.WriteLine (ex.Message);
				stdOut.Close();
				return;
			}
			 
			if (currentConfig == null)
			{
				stdOut.WriteLine ("Unable to read config file.");
				stdOut.Close();
				return;
			}

			ProfileConfig currentProfile = currentConfig.GetProfile(_profile);
			if (currentConfig == null)
			{
				stdOut.WriteLine ("Profile not found. " + _profile);
				stdOut.Close();
				return;
			}

			stdOut.WriteLine ("Using profile " + _profile);

			if ((currentProfile.overwrite == false) && (Encode.DestinationExists(_destination) == true))
			{
				stdOut.WriteLine ("Destination file exists.");
				stdOut.Close();
				return;
			}
			
			bool result = this.CropDetect(currentConfig.encodeCommand, String.Format(currentProfile.cropDetect, this._source, this._destination));
			
			if (result == true)
			{
				foreach (string currentPass in currentProfile.passes)
				{
					result = this.EncodeProcess(currentConfig.encodeCommand, String.Format(currentPass, this._source, this._destination, this._cropCommand));
					if (result == false)
					{
						break;
					}
				}

				foreach (string currentCleanup in currentProfile.cleanup)
				{
					result = this.CleanupProcess(String.Format(currentCleanup, this._source, this._destination));
					if (result == false)
					{
						break;
					}
				}
				
				if ((currentProfile.fourCC != null) && (currentProfile.fourCC.Length == 4))
				{
					FixRiff(_destination, currentProfile.fourCC);
				}
			}

			stdOut.Close();
		}
	}
}
