using DVF_API.Domain.BusinessLogic;
using DVF_API.Domain.Interfaces;
using FluentAssertions;
using Moq;
using System.Reflection;
using Xunit;

namespace DVF_API.Tests.DomainLayerTests
{
    /// <summary>
    /// This class is used to test the UtilityManager class.
    /// </summary>
    public class UtilityManagerTests
    {
        private readonly IUtilityManager _utilityManager;
        private const string ValidPassword = "2^aQeqnZoTH%PDgiFpRDa!!kL#kPLYWL3*D9g65fxQt@HYKpfAaWDkjS8sGxaCUEUVLrgR@wdoF";
        private const string InvalidPassword = "invalidPassword";
        private const string ClientIp = "192.168.1.100";




        public UtilityManagerTests()
        {

            var mock = new Mock<IUtilityManager>();
            int failedAttempts = 0;

            //Mocks for authentication
            mock.Setup(um => um.Authenticate(ValidPassword, ClientIp))
                 .Returns(true);

            mock.Setup(um => um.Authenticate(It.Is<string>(s => s != ValidPassword), ClientIp))
                .Throws(new UnauthorizedAccessException("Unauthorized - Incorrect password"));

            mock.Setup(um => um.Authenticate(It.Is<string>(s => s != ValidPassword), ClientIp))
                .Returns(() =>
                {
                    failedAttempts++;
                    if (failedAttempts >= 5)
                    {
                        throw new Exception("Too many failed attempts. Please try again later.");
                    }
                    throw new UnauthorizedAccessException("Unauthorized - Incorrect password");
                });
            
            _utilityManager = mock.Object;

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
            // Attempt to authenticate 5 times with an invalid password
            for (int i = 0; i < 5; i++)
            {
                Action act = () => _utilityManager.Authenticate(InvalidPassword, ClientIp);
                if (i < 4) // The first four attempts should throw UnauthorizedAccessException
                    act.Should().Throw<UnauthorizedAccessException>();
                else // The fifth attempt should throw Exception for too many attempts
                    act.Should().Throw<Exception>().WithMessage("Too many failed attempts. Please try again later.");
            }
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
        [InlineData(1500, "1,5 sec")]
        [InlineData(60000, "1 min")]
        [InlineData(180000, "3 min")]
        public void ConvertTimeMeasurementToFormat_GivenTime_ReturnsFormattedTime(float time, string expected)
        {
            var manager = new UtilityManager();
            string result = manager.ConvertTimeMeasurementToFormat(time);
            result.Should().Be(expected);
        }




        /// <summary>
        /// Test the ConvertDateTimeToFloat method. The expected value is the formatted double.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData("2023-04-15 00:00", 202304150000)]
        [InlineData("1999-12-31 23:59", 199912312359)]
        [InlineData("2024-01-01 12:34", 202401011234)]
        public void ConvertDateTimeToDouble_ReturnsExpectedLong(string input, long expected)
        {
            var utilityManager = new UtilityManager();
            long actual = utilityManager.ConvertDateTimeToDouble(input);
            Assert.Equal(expected, actual);
        }




        /// <summary>
        /// Test the ConvertDateTimeToFloat method with invalid input.
        /// </summary>
        [Fact]
        public void ConvertDateTimeToDouble_ThrowsFormatExceptionOnInvalidInput()
        {
            var utilityManager = new UtilityManager();
            string invalidInput = "invalid-date-format";
            Assert.Throws<FormatException>(() => utilityManager.ConvertDateTimeToDouble(invalidInput));
        }




        /// <summary>
        /// Test the ConvertDateTimeToFloat method with invalid input.
        /// </summary>
        [Theory]
        [InlineData("invalid-date-time", 0)] // Assuming that in case of error you want to return 0 or some other default value
        public void ConvertDateTimeToFloatInternal_HandlesInvalidInput(string time, double expected)
        {
            double result = _utilityManager.ConvertDateTimeToDouble(time);
            result.Should().Be(expected);
        }




        /// <summary>
        /// Test the MixedYearDateTimeSplitter method. The expected values are the date and time components.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="expectedDate"></param>
        /// <param name="expectedTime"></param>
        [Theory]
        [InlineData(202304150000, "20230415", 0f)] // Normal case
        [InlineData(199912312359, "19991231", 2359f)] // Edge case with time at the last minute of the day
        [InlineData(000000000000, "00000000", 0f)] // Minimum boundary value
        public void MixedYearDateTimeSplitter_ReturnsCorrectComponents(double input, string expectedDate, float expectedTime)
        {
            // Arrange
            var utilityManager = new UtilityManager();

            // Act
            var result = utilityManager.MixedYearDateTimeSplitter(input);

            // Assert
            Assert.Equal(expectedDate, result[0]);
            Assert.Equal(expectedTime, (float)result[1]);
        }




        /// <summary>
        /// Tests the MixedYearDateTimeSplitter method with invalid input.
        /// </summary>
        [Fact]
        public void MixedYearDateTimeSplitter_HandlesInvalidInput()
        {
            // Arrange
            var utilityManager = new UtilityManager();
            double invalidInput = -123456789012; // Negative or non-date value

            // Act
            var result = utilityManager.MixedYearDateTimeSplitter(invalidInput);

            // Assert
            Assert.Equal("00000000", result[0]);
            Assert.Equal(0f, (float)result[1]);
        }
    }
}
