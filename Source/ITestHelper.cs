using System;

namespace PSTesting
{
    /// <summary>
    /// Interface for test helpers that forces them to provide a SetUp and TearDown method which will be called
    /// before and after each test respectively.
    /// </summary>
    public interface ITestHelper
    {
        /// <summary>
        /// Sets the helper up before running a test.
        /// </summary>
        void SetUp();

        /// <summary>
        /// Tears the helper down after running a test.
        /// </summary>
        void TearDown();
    }
}

