// PSTesting - Classes to easily unit test Powershell and Pash related functionality
// Copyright (C) GRAU DATA 2013-2014
//
// Author(s): Stefan Burnicki <stefan.burnicki@graudata.com>
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was
// not distributed with this file, You can obtain one at
//  http://mozilla.org/MPL/2.0/.
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Management.Automation;
using System.Text;
using NUnit.Framework;

namespace PSTesting
{
	/// <summary>
	/// A base class for tests with an associated (optional) configuration file and easy access to test helpers
	/// and the TestShellInterface.
	/// </summary>
    public class TestBase
    {
		private const string DEFAULT_CONFIG_FILE_NAME = @"TestConfig.config";
		private string _configFileName;

        private AppSettingsSection _appSettings;
		/// <summary>
		/// Get the app settings from the configuration file. The instance is created on demand.
		/// </summary>
		/// <value>The app settings.</value>
        public AppSettingsSection AppSettings
        {
            get
            {
                if (_appSettings == null)
                {
                    ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
					configMap.ExeConfigFilename = _configFileName;
                    _appSettings = ConfigurationManager.OpenMappedExeConfiguration(configMap,
                                        ConfigurationUserLevel.None).AppSettings;
                }
                return _appSettings;
            }
        }

        private TestShellInterface _shell;
		/// <summary>
		/// Gets a shell interface to execute commands. The instance is created on demand.
		/// </summary>
		/// <value>The shell.</value>
        public TestShellInterface Shell
        {
            get
            {
                if (_shell == null)
                {
                    _shell = new TestShellInterface();
                }
                return _shell;
            }
        }

		/// <summary>
		/// Get the file system helper used for these tests. The instance is created on demand.
		/// </summary>
        private FileSystemTestHelper _fileSystemHelper;
        public FileSystemTestHelper FileSystemHelper
        {
            get
            {
                if (_fileSystemHelper == null)
                {
                    _fileSystemHelper = new FileSystemTestHelper();
                }
                return _fileSystemHelper;
            }
        }

		/// <summary>
		/// Deafult set up method that calls the SetUp method of the helpers, if used. Make sure to call this
		/// method when overriding the SetUp
		/// </summary>
		[SetUp]
		public virtual void SetUp()
		{
			if (_fileSystemHelper != null)
			{
				_fileSystemHelper.SetUp();
			}
		}

		/// <summary>
		/// Deafault tear down method that calls the TearDown method of the helpers, if used. Make sure to call this
		/// method when overriding the TearDown
		/// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            if (_fileSystemHelper != null)
            {
				_fileSystemHelper.TearDown();
            }
        }

		/// <summary>
		/// Initializes a new instance with DEFAULT_CONFIG_FILE_NAME and without SSL bypass.
		/// </summary>
		protected TestBase() : this(DEFAULT_CONFIG_FILE_NAME, false)
    	{
    	}    	

		/// <summary>
		/// Initializes a new instance with a config file name for the AppSettings and a flag to enable SSL bypass.
		/// </summary>
		/// <param name="configFileName">Name of the config file to use.</param>
		/// <param name="bypassSSL">If set to <c>true</c> bypass SSL.</param>
		protected TestBase(string configFileName, bool bypassSSL)
        {
			_configFileName = configFileName;
            // Should avoid problems with SSL and tests systems without valid certificate
			if (bypassSSL) {
				ServicePointManager.ServerCertificateValidationCallback +=
                	(sender, certificate, chain, sslPolicyErrors) => true;
			}
        }

		/// <summary>
		/// Utility function to join several strings with a system newline.
		/// </summary>
		/// <returns>The joined string.</returns>
		/// <param name="strs">Strings to join</param>
        public static string NewlineJoin(params string[] strs)
        {
            return String.Join(Environment.NewLine, strs) + Environment.NewLine;
        }
        
		/// <summary>
		/// Gets the cmdlet name of the cmdlet defined by cmdletType.
		/// </summary>
		/// <returns>The name of the cmdlet.</returns>
		/// <param name="cmdletType">The type of the cmdlet.</param>
        public static string CmdletName(Type cmdletType)
        {
            var attribute = Attribute.GetCustomAttribute(cmdletType, typeof(CmdletAttribute))
                as CmdletAttribute;
            return string.Format("{0}-{1}", attribute.VerbName, attribute.NounName);
        }

		/// <summary>
		/// Creates Powershell/Pash hashtable definition code from the specified object.
		/// </summary>
		/// <returns>The hashtable definition code.</returns>
		/// <param name="hashtable">Hashtable to generate code from.</param>
        public static string HashtableDefinition(IDictionary<string, string> hashtable)
        {
            var sb = new StringBuilder();
            sb.Append("@{{");
            foreach (var pair in hashtable)
            {
                sb.AppendFormat(" {0} = \"{1}\";", pair.Key, pair.Value);
            }
            sb.Remove(sb.Length - 1, 1); // remove last ;
            sb.Append("}; ");
            return sb.ToString();
        }
    }
}

