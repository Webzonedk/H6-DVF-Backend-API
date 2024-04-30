using DVF_API.Domain.BusinessLogic;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace DVF_API.Tests
{
    /// <summary>
    /// This class is used to test the UtilityManager class.
    /// </summary>
    public class UtilityManagerTests
    {
        private UtilityManager _utilityManager;
        private const string ValidPassword = "2^aQeqnZoTH%PDgiFpRDa!!kL#kPLYWL3*D9g65fxQt@HYKpfAaWDkjS8sGxaCUEUVLrgR@wdoF";
        private const string InvalidPassword = "invalidPassword";
        private const string ClientIp = "192.168.1.100";

        public UtilityManagerTests()
        {
            _utilityManager = new UtilityManager();
            // Ensure dictionary is clean before each test
            typeof(UtilityManager).GetField("_loginAttempts", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, new Dictionary<string, (DateTime, int)>());
        }




        /// <summary>
        /// Test the Authenticate method with a valid password.
        /// </summary>
        [Fact]
        public void Authenticate_ValidPassword_ReturnsTrue()
        {
            bool result = _utilityManager.Authenticate(ValidPassword, ClientIp);
            result.Should().BeTrue();
        }



        /// <summary>
        /// Test the Authenticate method with an invalid password.
        /// </summary>
        [Fact]
        public void Authenticate_InvalidPassword_ThrowsUnauthorizedAccessException()
        {
            Action act = () => _utilityManager.Authenticate(InvalidPassword, ClientIp);
            act.Should().Throw<UnauthorizedAccessException>().WithMessage("Unauthorized - Incorrect password");
        }




        /// <summary>
        /// Test the Authenticate method with multiple failed attempts.
        /// </summary>
        [Fact]
        public void Authenticate_MultipleFailedAttempts_ThrowsException()
        {
            for (int i = 0; i < 5; i++)
            {
                Action act = () => _utilityManager.Authenticate(InvalidPassword, ClientIp);
                act.Should().Throw<UnauthorizedAccessException>();
            }

            Action finalAct = () => _utilityManager.Authenticate(InvalidPassword, ClientIp);
            finalAct.Should().Throw<Exception>().WithMessage("Too many failed attempts. Please try again later.");
        }




        /// <summary>
        /// Test the GetModelSize method.
        /// </summary>
        [Fact]

        public void GetModelSize_ReturnsCorrectByteCount()
        {
            var manager = new UtilityManager();
            var obj = new { Name = "John", Age = 30 };

            int size = manager.GetModelSize(obj);
            size.Should().BeGreaterThan(0);
        }




        /// <summary>
        /// Test the ConvertBytesToMegabytes method. if the expected value is 1.0f, the result should be approximately 1.0f.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData(1048576, 1.0f)]  // 1 MB
        [InlineData(5242880, 5.0f)]  // 5 MB
        public void ConvertBytesToMegabytes_GivenBytes_ReturnsExpectedMegabytes(int bytes, float expected)
        {
            var manager = new UtilityManager();
            float result = manager.ConvertBytesToMegabytes(bytes);
            result.Should().BeApproximately(expected, 0.001f);
        }




        /// <summary>
        /// Test the ConvertBytesToGigabytes method. if the expected value is 1.0f, the result should be approximately 1.0f.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData(1073741824, 1.0f)]  // 1 GB
        [InlineData(5368709120, 5.0f)]  // 5 GB
        public void ConvertBytesToGigabytes_GivenBytes_ReturnsExpectedGigabytes(int bytes, float expected)
        {
            var manager = new UtilityManager();
            float result = manager.ConvertBytesToGigabytes(bytes);
            result.Should().BeApproximately(expected, 0.001f);
        }




        /// <summary>
        /// Test the ConvertBytesToFormat method. The expected value is the formatted string.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData(512, "512 bytes")]
        [InlineData(2048, "2 KB")]
        [InlineData(2097152, "2 MB")]
        [InlineData(2147483648, "2 GB")]
        public void ConvertBytesToFormat_GivenBytes_ReturnsFormattedString(long bytes, string expected)
        {
            var manager = new UtilityManager();
            string result = manager.ConvertBytesToFormat(bytes);
            result.Should().Be(expected);
        }




        /// <summary>
        /// Test the ConvertTimeMeasurementToFormat method. The expected value is the formatted string.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData(500, "500 ms")]
        [InlineData(1500, "1.5 sec")]
        [InlineData(60000, "1 min")]
        [InlineData(180000, "3 min")]
        public void ConvertTimeMeasurementToFormat_GivenTime_ReturnsFormattedTime(float time, string expected)
        {
            var manager = new UtilityManager();
            string result = manager.ConvertTimeMeasurementToFormat(time);
            result.Should().Be(expected);
        }














    }


}
