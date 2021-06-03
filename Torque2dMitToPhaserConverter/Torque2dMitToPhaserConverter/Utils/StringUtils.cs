using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.Utils
{
    public static class StringUtils
    {
        public static List<string> Tokenize(string input, char separator)
        {
            var result = new List<string>();

            var tokensWorkingCopy = input.Split(separator);

            foreach(var token in tokensWorkingCopy)
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    result.Add(token.Trim());
                }
            }

            return result;
        }

        public static string GetNextToken(string input, int startingCharIndex)
        {
            var lastCharIndex = startingCharIndex;

            for (var i = startingCharIndex; i < input.Length - startingCharIndex; i++)
            {
                if (char.IsWhiteSpace(input[i]))
                {
                    return input.Substring(startingCharIndex, i - startingCharIndex);
                }
            }

            return input.Substring(startingCharIndex);
        }

        public static string ConvertCharListToString(List<char> input)
        {
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static bool CharIsLegalNamingCharacter(char c)
        {
            if (char.IsLetterOrDigit(c))
            {
                return true;
            }

            if (c == '_')
            {
                return true;
            }

            return false;
        }

        public static bool CharIsUnderscoreOrLetter(char c)
        {
            if (char.IsLetter(c))
            {
                return true;
            }

            if (c == '_')
            {
                return true;
            }

            return false;
        }
    }
}
