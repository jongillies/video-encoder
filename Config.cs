using System;
using System.Collections;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoEncoder
{
	/// <summary>
	/// Summary description for Config.
	/// </summary>
	public class Config
	{
		public Config()
		{

		}
	}

	[Serializable]
	[XmlRoot("VideoEncoder")]
	public class VideoEncoderConfig
	{
		[XmlElement(ElementName="encodeCommand")]
		public string encodeCommand = null;
		[XmlElement(ElementName="defaultProfile")]
		public string defaultProfile = null;
		[XmlElement(ElementName="profile", Type = typeof(ProfileConfig))]
		public ArrayList profiles = new ArrayList();

		[XmlIgnore]
		public string filename = null;

		public static VideoEncoderConfig Load(string fileName)
		{
			if (fileName == null)
			{
				return null;
			}

			VideoEncoderConfig newConfig = null;
			try
			{
				XmlSerializer objSerializer = new XmlSerializer(typeof(VideoEncoderConfig));
				TextReader objReader = new StreamReader(fileName);
				newConfig = (VideoEncoderConfig)objSerializer.Deserialize(objReader);
				objReader.Close();
				newConfig.filename = fileName;
			} 
			catch (Exception ex)
			{
				throw (new Exception("Error reading config from file. " + ex.Message));
			}
			return newConfig;
		} // method

		public ProfileConfig GetProfile(string profileName)
		{
			if ((profileName == null) || (profileName.Length == 0) || (this.profiles == null) || (this.profiles.Count == 0))
			{
				return null;
			}
			foreach(ProfileConfig currentProfile in this.profiles)
			{
				if (String.Compare(currentProfile.profileName, profileName, true) == 0)
				{
					return currentProfile;
				}
			} // foreach
			return null;
		} // method

		public ProfileConfig GetProfile()
		{
			return GetProfile(this.defaultProfile);
		}
	} // class

	[Serializable]
	public class ProfileConfig
	{
		[XmlAttribute(AttributeName="profileName")]
		public string profileName = null;
		[XmlAttribute(AttributeName="overwrite")]
		public bool overwrite = false;
		[XmlElement(ElementName="fourCC", Type = typeof(string))]
		public string fourCC = null;
		[XmlElement(ElementName="description", Type = typeof(string))]
		public string description = null;
		[XmlElement(ElementName="cropDetect", Type = typeof(string))]
		public string cropDetect = null;
		[XmlElement(ElementName="pass", Type = typeof(string))]
		public ArrayList passes = new ArrayList();
		[XmlElement(ElementName="cleanup", Type = typeof(string))]
		public ArrayList cleanup = new ArrayList();
	} // class
} // namespace
