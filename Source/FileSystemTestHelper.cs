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
using System.IO;
using System.Text;
using NUnit.Framework.Constraints;
using NUnit.Framework;

namespace PSTesting
{
	/// <summary>
	/// Some test helpers for file system related operations. The helper is automatically set up and teared down
	/// when using TestBase
	/// </summary>
	public class FileSystemTestHelper : ITestHelper
    {
        private readonly List<string> _createdFiles;

#region ITestHelper implementation
		public void SetUp()
		{
			// nothing to do here
		}

		/// <summary>
		/// Deletes the registered temp files and created files
		/// </summary>
		public void TearDown()
		{
			for (int i = _createdFiles.Count - 1; i >= 0; i--)
			{
				File.Delete(_createdFiles[i]);
			}
			_createdFiles.Clear();
		}

#endregion

#region Constraints for tests
		/// <summary>
		/// Constraint to check whether the instance is the content of the UTF-8 file with the specified filename.
		/// </summary>
		/// <returns>The constraint to use with NUnit.</returns>
		/// <param name="filename">Filename of the file to check.</param>
        public EqualConstraint IsContentOf(string filename)
        {
            return IsContentOf(filename, Encoding.UTF8);
        }

		/// <summary>
		/// Constraint to check whether the instance is the content of the file with the specified filename and encoding.
		/// </summary>
		/// <returns>The constraint to use with NUnit.</returns>
		/// <param name="filename">Filename of the file to check.</param>
		/// <param name="encoding">The encoding of the file to check.</param>
        public EqualConstraint IsContentOf(string filename, Encoding encoding)
        {
            var content = File.ReadAllText(filename, encoding);
            return Is.EqualTo(content);
        }
#endregion

        public FileSystemTestHelper()
        {
            _createdFiles = new List<string>();
        }

		/// <summary>
		/// Registers a temporary file that should be automatically deleted on tear down.
		/// </summary>
		/// <param name="paths">Paths to add to the temporary files.</param>
        public void RegisterTempFile(params string[] paths)
        {
            _createdFiles.AddRange(paths);
        }

		/// <summary>
		/// Forgets about all the temporary files to be deleted that were created or registered.
		/// </summary>
        public void ForgetTempFiles()
        {
            _createdFiles.Clear();
        }

		/// <summary>
		/// Creates a temporary UTF-8 file at the given path with the given content. It will be automatically removed
		/// if not explicitly forgotten with ForgetTempFiles.
		/// </summary>
		/// <param name="path">Path of the new file.</param>
		/// <param name="content">Content of the new file.</param>
        public void CreateTempFile(string path, string content)
        {
            CreateTempFile(path, content, Encoding.UTF8);
        }

		/// <summary>
		/// Creates a temporary file at the given path with the given content and encoding . It will be automatically
		/// removed if not explicitly forgotten with ForgetTempFiles.
		/// </summary>
		/// <param name="path">Path of the new file.</param>
		/// <param name="content">Content of the new file.</param>
		/// <param name="encoding">Encoding of the new file.</param>
        public void CreateTempFile(string path, string content, Encoding encoding)
        {
            File.WriteAllText(path, content, encoding);
            _createdFiles.Add(path);
        }
    }
}

