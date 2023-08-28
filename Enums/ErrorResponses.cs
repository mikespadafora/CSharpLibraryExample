using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public sealed class ErrorResponses
    {
        private static List<ErrorResponses> myList = new List<ErrorResponses>();

        public readonly static ErrorResponses INVALID_MODULE_NAME = new ErrorResponses("01", "Invalid Module Name (no match found for module name – or duplicate name)");
        public readonly static ErrorResponses ILLEGAL_INDEX = new ErrorResponses("02", "Illegal Index (index value or quantity incorrect for specified module)");
        public readonly static ErrorResponses VALUE_OUT_OF_RANGE = new ErrorResponses("03", "Value is out-of-range (value is not permitted for the specified parameter)");
        public readonly static ErrorResponses UNKNOWN_ERROR = new ErrorResponses("99", "Unknown error");

        public String ErrorCode { get; private set; }
        public String ErrorMessage { get; private set; }

        private ErrorResponses(String ErrorCode, String ErrorMessage)
        {
            this.ErrorCode = ErrorCode;
            this.ErrorMessage = ErrorMessage;

            myList.Add(this);
        }

        public static ErrorResponses Find(String ErrorCode)
        {
            var item = myList.Find(x => x.ErrorCode == ErrorCode);

            if (item is ErrorResponses)
                return item;
            else
                return ErrorResponses.UNKNOWN_ERROR;
        }
    }
}