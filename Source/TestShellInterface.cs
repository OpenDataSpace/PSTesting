// PSTesting - Classes to easily unit test Powershell and Pash related functionality
// Copyright (C) GRAU DATA 2013-2015
//
// Author(s): Stefan Burnicki <stefan.burnicki@graudata.com>
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was
// not distributed with this file, You can obtain one at
//  http://mozilla.org/MPL/2.0/.
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System;
using System.Linq;
using NUnit.Framework.Constraints;
using NUnit.Framework;

namespace PSTesting
{
    /// <summary>
    /// When the Shell executes a command and returns errors, this exception will be thrown.
    /// </summary>
    public class ShellExecutionHasErrorsException : Exception
    {
        /// <summary>
        /// Retrieve the errors that occured during the command
        /// </summary>
        /// <value>The errors.</value>
        public Collection<ErrorRecord> Errors { get; private set; }

        public ShellExecutionHasErrorsException(Collection<ErrorRecord> errors) : base()
        {
            Errors = errors;
        }
    }

    /// <summary>
    /// An interface that uses Powershell/Pash API in order to execute some commands.
    /// It brings some utility functions like automatic module loading, exceptions on errors, NUnit constraints,
    /// and more
    /// </summary>
    public class TestShellInterface
    {
        public Runspace LastRunspace { get; set; }
        public Collection<object> LastResults { get; set; }
        public Collection<ErrorRecord> LastErrors { get; set; }

        private string[] _preExecutionCmds;
        private string[] _postExecutionCmds;
        private string _modulePath;

        /// <summary>
        /// Initializes a new instance. No module will be loaded automatically
        /// </summary>
        public TestShellInterface() : this((string) null)
        {
        }

        /// <summary>
        /// Initializes a new instance. The shell interface will look for an assembly containing the given type
        /// and load this assembly automatically as a module before each execution in a new runspace.
        /// </summary>
        /// <param name="typeInModule">The type used to look for the assembly module.</param>
        public TestShellInterface(Type typeInModule) : this(new Uri(typeInModule.Assembly.CodeBase).LocalPath)
        {
        }

        /// <summary>
        /// Initializes a new instance. The shell interface will automatically load the module at the given path before
        /// each execution in a new runspace.
        /// </summary>
        /// <param name="modulePath">Path to the module to load</param>
        public TestShellInterface(string modulePath)
        {
            _modulePath = modulePath;
        }

        /// <summary>
        /// Set commands that should be executed before each execution in a new runspace. This is useful for example to
        /// automatically connect to a server before running a unit test
        /// </summary>
        /// <param name="commands">Commands to be executed</param>
        public void SetPreExecutionCommands(params string[] commands)
        {
            _preExecutionCmds = commands;
        }

        /// <summary>
        /// Set commands that should be executed after each execution in a new runspace. This is useful for example to
        /// automatically disconnect from a server after running a unit test
        /// </summary>
        /// <param name="commands">Commands to be executed</param>
        public void SetPostExecutionCommands(params string[] commands)
        {
            _postExecutionCmds = commands;
        }

        /// <summary>
        /// Execute the specified commands in a new runspace. The set pre- and postexecution commands are pre- and
        /// appended to the given commands.
        /// </summary>
        /// <returns>The results of the operation (unpacked from PSObject).</returns>
        /// <param name="commands">The commands to be executed.</param>
        public Collection<object> Execute(params string[] commands)
        {
            if (LastRunspace != null)
            {
                LastRunspace.Close();
            }
            LastRunspace = RunspaceFactory.CreateRunspace();
            LastRunspace.Open();

            if (_modulePath != null)
            {
                LoadModule(_modulePath);
            }

            var allCmds = JoinCommands(_preExecutionCmds) + JoinCommands(commands) + JoinCommands(_postExecutionCmds);
            return ExecuteInExistingRunspace(allCmds);
        }

        /// <summary>
        /// Executes the commands in the last used runspace. This doesn't run the pre- and postexecution commands
        /// gain, neither does it re-import the module.
        /// </summary>
        /// <returns>The results of the operation (unpacked from PSObject).</returns>
        /// <param name="commands">The commands to be executed.</param>
        public Collection<object> ExecuteInExistingRunspace(params string[] commands)
        {
            CheckLastRunspaceExists();
            Collection<PSObject> results = null;
            LastResults = new Collection<object>();
            LastErrors = new Collection<ErrorRecord>();

            using (var pipeline = LastRunspace.CreatePipeline())
            {
                pipeline.Commands.AddScript(JoinCommands(commands), false);
                results = pipeline.Invoke();
                LastErrors = new Collection<ErrorRecord>(
                    (from errObj in pipeline.Error.NonBlockingRead()
                                where errObj is PSObject
                                select ((PSObject) errObj).BaseObject as ErrorRecord
                    ).ToList()
                );
            }

            foreach (var curPSObject in results)
            {
                if (curPSObject == null)
                {
                    LastResults.Add(null);
                }
                else
                {
                    LastResults.Add(curPSObject.BaseObject);
                }
            }

            if (LastErrors.Count > 0)
            {
                throw new ShellExecutionHasErrorsException(LastErrors);
            }
            return LastResults;
        }

        /// <summary>
        /// Gets the value of a variable from the last used runspace.
        /// </summary>
        /// <returns>The variable value (unpacked from PSObject).</returns>
        /// <param name="variableName">The name of the variable to get</param>
        public object GetVariableValue(string variableName)
        {
            CheckLastRunspaceExists();
            object variable = LastRunspace.SessionStateProxy.GetVariable(variableName);
            var pSObject = variable as PSObject;
            if (pSObject != null)
            {
                variable = pSObject.BaseObject;
            }
            return variable;
        }

        /// <summary>
        /// Loads a module into the last used runspace.
        /// </summary>
        /// <param name="modulePath">The path to the module</param>
        public void LoadModule(string modulePath)
        {
            CheckLastRunspaceExists();
            string cmd = String.Format("Import-Module '{0}'", modulePath);
            using (var pipeline = LastRunspace.CreatePipeline())
            {
                pipeline.Commands.AddScript(String.Format(cmd, modulePath));
                try
                {
                    pipeline.Invoke();
                }
                catch (MethodInvocationException e)
                {
                    throw new RuntimeException(String.Format(
                        "Failed to import module '{0}'. Didn't you build it?", modulePath), e);
                }
            }
        }

#region Constraints for Nunit
        /// <summary>
        /// Constraint that checks whether the compared instance is the value of variable the specified variableName.
        /// </summary>
        /// <returns>The constraint to be used with NUnit.</returns>
        /// <param name="variableName">The name of the variable to check.</param>
        public EqualConstraint IsValueOfVariable(string variableName)
        {
            return Is.EqualTo(GetVariableValue(variableName));
        }

        /// <summary>
        /// Constraint that checks whether the compared instance is the result of the execution of the specified command.
        /// </summary>
        /// <returns>The constraint to be used with NUnit.</returns>
        /// <param name="command">The command to be executed.</param>
        public EqualConstraint IsResultOf(string command)
        {
            var results = Execute(command);
            return Is.EqualTo(results).AsCollection;
        }
#endregion

        private void CheckLastRunspaceExists() {
            if (LastRunspace == null) {
                throw new RuntimeException("Cannot execute operation in last runspace, because there is none!");
            }
        }

        private static string JoinCommands(string[] cmds)
        {
            return cmds == null ? "" : String.Join (";" + Environment.NewLine, cmds);
        }
    }
}
