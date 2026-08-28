using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Test.HelperClasses
{
    public class BuilderHelper
    {
        public static string GenerateForString(decimal[] decimals, Guid userId)
        {
            StringBuilder builder = new StringBuilder();
            foreach (decimal dec in decimals)
            {
                builder.Append(GenerateForString(dec, userId));
            }
            return builder.ToString();

        }

        public static string GenerateForString(decimal dec, Guid userId)
        {
            return $"({dec},{userId})";
        }


    }
}
