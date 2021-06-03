using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torque2dMitToPhaserConverter.Utils;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public static class InputRules
    {
        // NOTE: Will have to check 'externally' for any ClassMethod CodeBlocks, and handle the ClassMethods accordingly
        public static List<CodeBlock> GenerateCodeBlockFromLine(string inputCodeLine, ref bool currentlyInCommentBlock, List<CodeBlock> currentCompletedCodeBlockList)
        {
            return TokenizeIntoCodeTokens(inputCodeLine, ref currentlyInCommentBlock, currentCompletedCodeBlockList);
        }

        private static List<CodeBlock> TokenizeIntoCodeTokens(string inputCodeLine, ref bool currentlyInCommentBlock, List<CodeBlock> currentCompletedCodeBlockList)
        {
            var resultList = new List<CodeBlock>();
            var currentlyInCommentBlockCopy = currentlyInCommentBlock;
            var currentlyIsANumericValueToken = false;
            var currentlyIsAFunctionOrClassMethodToken = false;
            var currentlyIsABasicCodeToken = false;
            var currentlyIsALocalOrGlobalVariable = false;
            var currentlyIsAStringValueToken = false;

            int charIndex;
            var currentToken = new List<char>();
            var currentTokenType = typeof(CodeBlock);


            for (charIndex = 0; charIndex < inputCodeLine.Length; charIndex++)
            {
                if (currentlyIsAStringValueToken)
                {
                    if (inputCodeLine[charIndex] == '"')
                    {
                        // NOTE: If the previous character is an escape (ie a '\' char) then handle accordingly (do not want to generate string token yet)
                        if (charIndex > 0)
                        {
                            if (inputCodeLine[charIndex - 1] == '\\')
                            {
                                // will add character to the currentToken (the double quotes char that is)
                                currentToken.Add(inputCodeLine[charIndex]);
                                continue;
                            }
                        }

                        currentToken.Add('"');
                        resultList = GenerateToken(currentToken, currentTokenType, resultList); // adds string value token

                        currentToken = new List<char>();
                        currentTokenType = typeof(CodeBlock);

                        currentlyIsAStringValueToken = false;

                        continue;
                    }

                    // else, will add character to the currentToken
                    currentToken.Add(inputCodeLine[charIndex]);
                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '*' && inputCodeLine[charIndex + 1] == '/')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('*');
                    currentToken.Add('/');

                    currentTokenType = typeof(CommentBlockEnd);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one more once the continue is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    currentlyInCommentBlockCopy = false;

                    continue;
                }

                if (currentlyInCommentBlockCopy)
                {
                    // just take character, will be a TakeCodeBlockAsIs until CommentBlockEnd is reached
                    currentToken.Add(inputCodeLine[charIndex]);
                    currentTokenType = typeof(TakeCodeBlockAsIs);
                    continue;
                }

                if (currentlyIsANumericValueToken)
                {
                    if (char.IsNumber(inputCodeLine[charIndex]) || inputCodeLine[charIndex] == '.')
                    {
                        currentToken.Add(inputCodeLine[charIndex]);
                        continue;
                    }
                    else if (inputCodeLine[charIndex] == 'd' || inputCodeLine[charIndex] == 'b')
                    {
                        // currently not handling 'd' (decimal) or 'b' (binary) values.  TODO!
                        continue;
                    }
                    else
                    {
                        // TODO - should somehow handle exponential numbers also at some point, but will do later if needed


                        resultList = GenerateToken(currentToken, currentTokenType, resultList); // generate numeric value code token

                        currentlyIsANumericValueToken = false;

                        charIndex = charIndex - 1;

                        currentToken = new List<char>();
                        currentTokenType = typeof(CodeBlock);

                        continue;
                    }
                }

                if (currentlyIsABasicCodeToken)
                {
                    if (StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex]))
                    {
                        currentToken.Add(inputCodeLine[charIndex]);
                        continue;
                    }

                    // NOTE:  When generating a BasicCodeToken, if the value of the BasicCodeToken is either 'superclass' or 'class',
                    // then will want to lookup previous tokens (in resultList) until we find a OpenRoundBracket, at which then the
                    // preceding token should be also a BasicCodeToken and we will then assign the Class/SuperClass of that
                    // BasicCodeToken with the value of the superclass / class field of this token
                    if (StringUtils.ConvertCharListToString(currentToken) == "superclass")
                    {
                        var superclassVal = GetSuperclassOrClassValue(inputCodeLine, charIndex);

                        var lastBasicCodeToken = GetLastObjectBasicCodeTokenIndex(resultList, currentCompletedCodeBlockList);
                        lastBasicCodeToken.SuperClass = superclassVal;

                        charIndex = inputCodeLine.IndexOf(superclassVal) + superclassVal.Length;

                        currentToken = new List<char>();
                        currentTokenType = typeof(CodeBlock);

                        // want to consume following semicolon.  Do not store as a token
                        var semicolonIdx = inputCodeLine.IndexOf(";", charIndex);

                        if (semicolonIdx != -1)
                        {
                            charIndex = semicolonIdx;
                        }

                        currentlyIsABasicCodeToken = false;

                        continue;
                    }
                    else if (StringUtils.ConvertCharListToString(currentToken) == "class")
                    {
                        var classVal = GetSuperclassOrClassValue(inputCodeLine, charIndex);

                        var lastBasicCodeToken = GetLastObjectBasicCodeTokenIndex(resultList, currentCompletedCodeBlockList);
                        lastBasicCodeToken.Class = classVal;

                        charIndex = inputCodeLine.IndexOf(classVal) + classVal.Length;

                        currentToken = new List<char>();
                        currentTokenType = typeof(CodeBlock);

                        // want to consume following semicolon.  Do not store as a token
                        var semicolonIdx = inputCodeLine.IndexOf(";", charIndex);

                        if (semicolonIdx != -1)
                        {
                            charIndex = semicolonIdx;
                        }

                        currentlyIsABasicCodeToken = false;

                        continue;
                    }
                    else if (StringUtils.ConvertCharListToString(currentToken).ToLower().Contains("scenewindow"))
                    {
                        // might be a line related to the scene window object (in Torque 2D).  Add warning comment for developer to remove this line (if option is enabled in the Converter)
                        if (GlobalVars.AddSceneWindowRemovalWarningComments)
                        {
                            resultList = GenerateToken("/*".ToList(), typeof(CommentBlockBegin), resultList);
                            resultList = GenerateToken("WARNING - SceneWindow does not translate to a Phaser object.  Consider removing this?".ToList(), typeof(TakeCodeBlockAsIs) , resultList);
                            resultList = GenerateToken("/*".ToList(), typeof(CommentBlockEnd), resultList);
                            resultList = GenerateToken("\n".ToList(), typeof(NewLineCharacter), resultList);
                        }
                    }
                    else if (StringUtils.ConvertCharListToString(currentToken).ToLower().Contains("actionmap"))
                    {
                        // Add warning comment for developer to modify this functionality manually (if option is enabled in the Converter)
                        if (GlobalVars.AddActionMapManualConversionWarningComments)
                        {
                            resultList = GenerateToken("/*".ToList(), typeof(CommentBlockBegin), resultList);
                            resultList = GenerateToken("WARNING - ActionMap (ie keyboard input) needs to be handled manually.  Please convert code as needed.".ToList(), typeof(TakeCodeBlockAsIs), resultList);
                            resultList = GenerateToken("/*".ToList(), typeof(CommentBlockEnd), resultList);
                            resultList = GenerateToken("\n".ToList(), typeof(NewLineCharacter), resultList);
                        }
                    }

                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generate basic code token

                    currentlyIsABasicCodeToken = false;

                    charIndex = charIndex - 1;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }

                if (inputCodeLine[charIndex] == ',')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add(',');

                    currentTokenType = typeof(Comma);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    currentlyIsALocalOrGlobalVariable = false;

                    continue;
                }

                if (
                    ((inputCodeLine.Length - charIndex) > 4 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'n' && inputCodeLine[charIndex + 2] == 'e' && inputCodeLine[charIndex + 3] == 'w' && char.IsWhiteSpace(inputCodeLine[charIndex + 4])) ||
                    ((inputCodeLine.Length - charIndex) > 3 && inputCodeLine[charIndex] == 'n' && inputCodeLine[charIndex + 1] == 'e' && inputCodeLine[charIndex + 2] == 'w' && char.IsWhiteSpace(inputCodeLine[charIndex + 3]))
                )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('n');
                    currentToken.Add('e');
                    currentToken.Add('w');

                    currentTokenType = typeof(NewOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentToken.Add(inputCodeLine[charIndex + 3]);

                    currentTokenType = typeof(WhitespaceCharacter);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 3;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    resultList = SetPhaserObjectTypeOfPreviousToken(resultList, inputCodeLine, charIndex + 1, currentCompletedCodeBlockList);

                    continue;
                }
                else if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add(inputCodeLine[charIndex]);

                    currentTokenType = typeof(WhitespaceCharacter);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generate single whitespace character code token, reset token values, then break out of for loop

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    currentlyIsALocalOrGlobalVariable = false;

                    continue;
                }
                else if (currentlyIsAFunctionOrClassMethodToken)
                {
                    var tokenToCheckForEitherFunctionOrClassMethod = StringUtils.GetNextToken(inputCodeLine, charIndex);

                    var checkForOpenBracketIdx = tokenToCheckForEitherFunctionOrClassMethod.IndexOf("(");

                    if (checkForOpenBracketIdx != -1)
                    {
                        tokenToCheckForEitherFunctionOrClassMethod = 
                            tokenToCheckForEitherFunctionOrClassMethod.Substring(0, checkForOpenBracketIdx);
                    }

                    currentToken = tokenToCheckForEitherFunctionOrClassMethod.ToList();

                    if (tokenToCheckForEitherFunctionOrClassMethod.Contains("::"))
                    {
                        // is a class method; handle accordingly
                        currentTokenType = typeof(ClassMethod);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);
                    }
                    else
                    {
                        // is function declaration; handle accordingly
                        currentTokenType = typeof(FunctionDeclaration);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);
                    }

                    if (inputCodeLine.Substring(charIndex).IndexOf('(') == -1)
                    {
                        charIndex = inputCodeLine.Length - 1;
                    }
                    else
                    {
                        charIndex = charIndex + inputCodeLine.Substring(charIndex).IndexOf('(') - 1;
                    }

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    currentlyIsAFunctionOrClassMethodToken = false;

                    continue;
                }
                else if (currentlyIsALocalOrGlobalVariable)
                {
                    if (StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex]))
                    {
                        currentToken.Add(inputCodeLine[charIndex]);
                        continue;
                    }

                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generate local or global variable token

                    charIndex = charIndex - 1;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    currentlyIsALocalOrGlobalVariable = false;

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '/' && inputCodeLine[charIndex + 1] == '/')
                {
                    // TODO: make comment single line codeblock.  Will extend as far as the current line
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = inputCodeLine.Substring(charIndex).ToList();
                    currentTokenType = typeof(SingleLineComment);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generate single line comment code token, reset token values, then break out of for loop

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    break;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '/' && inputCodeLine[charIndex + 1] == '*')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('/');
                    currentToken.Add('*');

                    currentTokenType = typeof(CommentBlockBegin);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(TakeCodeBlockAsIs);

                    currentlyInCommentBlockCopy = true;

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '=' && inputCodeLine[charIndex + 1] == '=')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('=');
                    currentToken.Add('=');

                    currentTokenType = typeof(BooleanEqualsOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '!' && inputCodeLine[charIndex + 1] == '=')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('!');
                    currentToken.Add('=');

                    currentTokenType = typeof(BooleanNotEqualsOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '<' && inputCodeLine[charIndex + 1] == '=')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('<');
                    currentToken.Add('=');

                    currentTokenType = typeof(BooleanLessThanOrEqualToOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '>' && inputCodeLine[charIndex + 1] == '=')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('>');
                    currentToken.Add('=');

                    currentTokenType = typeof(BooleanGreaterThanOrEqualToOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '$' && inputCodeLine[charIndex + 1] == '=')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('$');
                    currentToken.Add('=');

                    currentTokenType = typeof(StringEqualOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 2 && inputCodeLine[charIndex] == '!' && inputCodeLine[charIndex + 1] == '$' && inputCodeLine[charIndex + 2] == '=')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('!');
                    currentToken.Add('$');
                    currentToken.Add('=');

                    currentTokenType = typeof(StringNotEqualOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 2; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '+' && inputCodeLine[charIndex + 1] == '+')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('+');
                    currentToken.Add('+');

                    currentTokenType = typeof(PostIncrementOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '-' && inputCodeLine[charIndex + 1] == '-')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('-');
                    currentToken.Add('-');

                    currentTokenType = typeof(PostDecrementOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '&' && inputCodeLine[charIndex + 1] == '&')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('&');
                    currentToken.Add('&');

                    currentTokenType = typeof(BooleanAndOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '|' && inputCodeLine[charIndex + 1] == '|')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('|');
                    currentToken.Add('|');

                    currentTokenType = typeof(BooleanOrOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '<' && inputCodeLine[charIndex + 1] == '<')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('<');
                    currentToken.Add('<');

                    currentTokenType = typeof(BitwiseLeftShiftOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '>' && inputCodeLine[charIndex + 1] == '>')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('>');
                    currentToken.Add('>');

                    currentTokenType = typeof(BitwiseRightShiftOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '=')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('=');

                    currentTokenType = typeof(EqualsOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '+')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('+');

                    currentTokenType = typeof(AddOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '-')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('-');

                    currentTokenType = typeof(SubtractOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '/')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('/');

                    currentTokenType = typeof(DivideOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '*')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('*');

                    currentTokenType = typeof(MultiplyOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '&')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('&');

                    currentTokenType = typeof(BitwiseAndOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '|')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('|');

                    currentTokenType = typeof(BitwiseOrOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '^')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('^');

                    currentTokenType = typeof(BitwiseXorOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '~')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('~');

                    currentTokenType = typeof(BitwiseComplementOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '<')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('<');

                    currentTokenType = typeof(BooleanLessThanOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '>')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('>');

                    currentTokenType = typeof(BooleanGreaterThanOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '!')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('!');

                    currentTokenType = typeof(BooleanNotOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == ';')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add(';');

                    currentTokenType = typeof(Semicolon);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '%')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (inputCodeLine.Length > charIndex + 1)
                    {
                        if (StringUtils.CharIsUnderscoreOrLetter(inputCodeLine[charIndex + 1]))
                        {
                            resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                            currentToken = new List<char>();
                            currentToken.Add('%');

                            currentTokenType = typeof(LocalVariable);

                            currentlyIsALocalOrGlobalVariable = true;

                            continue;
                        }
                    }

                    currentToken = new List<char>();
                    currentToken.Add('%');

                    currentTokenType = typeof(ModulusOperator);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if ((inputCodeLine.Length - charIndex) > 1 && inputCodeLine[charIndex] == '$' && StringUtils.CharIsUnderscoreOrLetter(inputCodeLine[charIndex + 1]))
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('$');

                    currentTokenType = typeof(GlobalVariable);

                    currentlyIsALocalOrGlobalVariable = true;

                    continue;
                }
                else if (inputCodeLine[charIndex] == '"')
                {
                    // must check to see if this is a beginning of a new string/vector, or whether this is the end of string/vector
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (currentTokenType != typeof(StringValue))
                    {
                        currentToken = new List<char>();
                        currentToken.Add('"');

                        currentTokenType = typeof(StringValue);

                        currentlyIsAStringValueToken = true;

                        continue;
                    }

                    currentToken.Add('"');
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // adds string value token

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '{')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('{');

                    currentTokenType = typeof(OpenCurlyBracket);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '}')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('}');

                    currentTokenType = typeof(ClosedCurlyBracket);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 3 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'i' && inputCodeLine[charIndex + 2] == 'f' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 3]))) ||
                    ((inputCodeLine.Length - charIndex) > 2 && inputCodeLine[charIndex] == 'i' && inputCodeLine[charIndex + 1] == 'f' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 2])))
                    )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('i');
                    currentToken.Add('f');

                    currentTokenType = typeof(IfKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '(')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('(');

                    currentTokenType = typeof(OpenRoundBracket);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == ')')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add(')');

                    currentTokenType = typeof(ClosedRoundBracket);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 7 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'e' && inputCodeLine[charIndex + 2] == 'l' && inputCodeLine[charIndex + 3] == 's' && inputCodeLine[charIndex + 4] == 'e' &&
                     char.IsWhiteSpace(inputCodeLine[charIndex + 5]) && inputCodeLine[charIndex + 6] == 'i' && inputCodeLine[charIndex + 7] == 'f') ||
                    ((inputCodeLine.Length - charIndex) > 6 && inputCodeLine[charIndex] == 'e' && inputCodeLine[charIndex + 1] == 'l' && inputCodeLine[charIndex + 2] == 's' && inputCodeLine[charIndex + 3] == 'e' &&
                     char.IsWhiteSpace(inputCodeLine[charIndex + 4]) && inputCodeLine[charIndex + 5] == 'i' && inputCodeLine[charIndex + 6] == 'f')
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('e');
                    currentToken.Add('l');
                    currentToken.Add('s');
                    currentToken.Add('e');
                    currentToken.Add(' ');
                    currentToken.Add('i');
                    currentToken.Add('f');

                    currentTokenType = typeof(ElseIfKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 6;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 5 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'e' && inputCodeLine[charIndex + 2] == 'l' && inputCodeLine[charIndex + 3] == 's' && inputCodeLine[charIndex + 4] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 5]))) ||
                    ((inputCodeLine.Length - charIndex) > 4 && inputCodeLine[charIndex] == 'e' && inputCodeLine[charIndex + 1] == 'l' && inputCodeLine[charIndex + 2] == 's' && inputCodeLine[charIndex + 3] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 4])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('e');
                    currentToken.Add('l');
                    currentToken.Add('s');
                    currentToken.Add('e');

                    currentTokenType = typeof(ElseKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 3;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 7 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 's' && inputCodeLine[charIndex + 2] == 'w' && inputCodeLine[charIndex + 3] == 'i' && inputCodeLine[charIndex + 4] == 't' && inputCodeLine[charIndex + 5] == 'c' && inputCodeLine[charIndex + 6] == 'h' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 7]))) ||
                    ((inputCodeLine.Length - charIndex) > 6 && inputCodeLine[charIndex] == 's' && inputCodeLine[charIndex + 1] == 'w' && inputCodeLine[charIndex + 2] == 'i' && inputCodeLine[charIndex + 3] == 't' && inputCodeLine[charIndex + 4] == 'c' && inputCodeLine[charIndex + 5] == 'h' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 6]))) ||
                    ((inputCodeLine.Length - charIndex) > 8 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 's' && inputCodeLine[charIndex + 2] == 'w' && inputCodeLine[charIndex + 3] == 'i' && inputCodeLine[charIndex + 4] == 't' && inputCodeLine[charIndex + 5] == 'c' && inputCodeLine[charIndex + 6] == 'h' && inputCodeLine[charIndex + 7] == '$' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 8]))) ||
                    ((inputCodeLine.Length - charIndex) > 7 && inputCodeLine[charIndex] == 's' && inputCodeLine[charIndex + 1] == 'w' && inputCodeLine[charIndex + 2] == 'i' && inputCodeLine[charIndex + 3] == 't' && inputCodeLine[charIndex + 4] == 'c' && inputCodeLine[charIndex + 5] == 'h' && inputCodeLine[charIndex + 6] == '$' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 7])))
                     )
                {
                    bool dollarSwitchKeyword = false;

                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;

                        if ((inputCodeLine.Length - (charIndex + 1)) > 7 && inputCodeLine[charIndex + 7] == '$')
                        {
                            dollarSwitchKeyword = true;
                        }
                    }
                    else
                    {
                        if ((inputCodeLine.Length - (charIndex + 1)) > 6 && inputCodeLine[charIndex + 6] == '$')
                        {
                            dollarSwitchKeyword = true;
                        }
                    }

                    currentToken = new List<char>();

                    /* Don't really need this anyways....not going to bother coding it properly
                    currentToken.Add('s');
                    currentToken.Add('w');
                    currentToken.Add('i');
                    currentToken.Add('t');
                    currentToken.Add('c');
                    currentToken.Add('h');
                    if (dollarSwitchKeyword) { currentToken.Add('$'); } ???
                    */

                    currentTokenType = typeof(SwitchKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 5;

                    if (dollarSwitchKeyword)
                    {
                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 4 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'f' && inputCodeLine[charIndex + 2] == 'o' && inputCodeLine[charIndex + 3] == 'r' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 4]))) ||
                    ((inputCodeLine.Length - charIndex) > 3 && inputCodeLine[charIndex] == 'f' && inputCodeLine[charIndex + 1] == 'o' && inputCodeLine[charIndex + 2] == 'r' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 3])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('f');
                    currentToken.Add('o');
                    currentToken.Add('r');

                    currentTokenType = typeof(ForKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 2;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 6 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'w' && inputCodeLine[charIndex + 2] == 'h' && inputCodeLine[charIndex + 3] == 'i' && inputCodeLine[charIndex + 4] == 'l' && inputCodeLine[charIndex + 5] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 6]))) ||
                    ((inputCodeLine.Length - charIndex) > 5 && inputCodeLine[charIndex] == 'w' && inputCodeLine[charIndex + 1] == 'h' && inputCodeLine[charIndex + 2] == 'i' && inputCodeLine[charIndex + 3] == 'l' && inputCodeLine[charIndex + 4] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 5])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('w');
                    currentToken.Add('h');
                    currentToken.Add('i');
                    currentToken.Add('l');
                    currentToken.Add('e');

                    currentTokenType = typeof(WhileKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 4;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 5 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'c' && inputCodeLine[charIndex + 2] == 'a' && inputCodeLine[charIndex + 3] == 's' && inputCodeLine[charIndex + 4] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 5]))) ||
                    ((inputCodeLine.Length - charIndex) > 4 && inputCodeLine[charIndex] == 'c' && inputCodeLine[charIndex + 1] == 'a' && inputCodeLine[charIndex + 2] == 's' && inputCodeLine[charIndex + 3] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 4])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('c');
                    currentToken.Add('a');
                    currentToken.Add('s');
                    currentToken.Add('e');

                    currentTokenType = typeof(CaseKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 3;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 8 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'd' && inputCodeLine[charIndex + 2] == 'e' && inputCodeLine[charIndex + 3] == 'f' && inputCodeLine[charIndex + 4] == 'a' && inputCodeLine[charIndex + 5] == 'u' && inputCodeLine[charIndex + 6] == 'l' && inputCodeLine[charIndex + 7] == 't' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 8]))) ||
                    ((inputCodeLine.Length - charIndex) > 7 && inputCodeLine[charIndex] == 'd' && inputCodeLine[charIndex + 1] == 'e' && inputCodeLine[charIndex + 2] == 'f' && inputCodeLine[charIndex + 3] == 'a' && inputCodeLine[charIndex + 4] == 'u' && inputCodeLine[charIndex + 5] == 'l' && inputCodeLine[charIndex + 6] == 't' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 7])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('d');
                    currentToken.Add('e');
                    currentToken.Add('f');
                    currentToken.Add('a');
                    currentToken.Add('u');
                    currentToken.Add('l');
                    currentToken.Add('t');

                    currentTokenType = typeof(DefaultKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 6;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 6 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'b' && inputCodeLine[charIndex + 2] == 'r' && inputCodeLine[charIndex + 3] == 'e' && inputCodeLine[charIndex + 4] == 'a' && inputCodeLine[charIndex + 5] == 'k' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 6]))) ||
                    ((inputCodeLine.Length - charIndex) > 5 && inputCodeLine[charIndex] == 'b' && inputCodeLine[charIndex + 1] == 'r' && inputCodeLine[charIndex + 2] == 'e' && inputCodeLine[charIndex + 3] == 'a' && inputCodeLine[charIndex + 4] == 'k' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 5])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('b');
                    currentToken.Add('r');
                    currentToken.Add('e');
                    currentToken.Add('a');
                    currentToken.Add('k');

                    currentTokenType = typeof(BreakKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 4;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 9 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'c' && inputCodeLine[charIndex + 2] == 'o' && inputCodeLine[charIndex + 3] == 'n' && inputCodeLine[charIndex + 4] == 't' && inputCodeLine[charIndex + 5] == 'i' && inputCodeLine[charIndex + 6] == 'n' && inputCodeLine[charIndex + 7] == 'u' && inputCodeLine[charIndex + 8] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 9]))) ||
                    ((inputCodeLine.Length - charIndex) > 8 && inputCodeLine[charIndex] == 'c' && inputCodeLine[charIndex + 1] == 'o' && inputCodeLine[charIndex + 2] == 'n' && inputCodeLine[charIndex + 3] == 't' && inputCodeLine[charIndex + 4] == 'i' && inputCodeLine[charIndex + 5] == 'n' && inputCodeLine[charIndex + 6] == 'u' && inputCodeLine[charIndex + 7] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 8])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('c');
                    currentToken.Add('o');
                    currentToken.Add('n');
                    currentToken.Add('t');
                    currentToken.Add('i');
                    currentToken.Add('n');
                    currentToken.Add('u');
                    currentToken.Add('e');

                    currentTokenType = typeof(ContinueKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 7;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == '@')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('@');

                    currentTokenType = typeof(StringConcatBasic);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 2 && charIndex > 0 && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex - 1])) && inputCodeLine[charIndex] == 'N' && inputCodeLine[charIndex + 1] == 'L' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 2]))) ||
                    ((inputCodeLine.Length - charIndex) > 2 && inputCodeLine[charIndex] == 'N' && inputCodeLine[charIndex + 1] == 'L' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 2])))
                    )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('N');
                    currentToken.Add('L');

                    currentTokenType = typeof(StringConcatNewline);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 1; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 3 && charIndex > 0 && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex - 1])) && inputCodeLine[charIndex] == 'T' && inputCodeLine[charIndex + 1] == 'A' && inputCodeLine[charIndex + 2] == 'B' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 3]))) ||
                    ((inputCodeLine.Length - charIndex) > 3 && inputCodeLine[charIndex] == 'T' && inputCodeLine[charIndex + 1] == 'A' && inputCodeLine[charIndex + 2] == 'B' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 3])))
                    )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('T');
                    currentToken.Add('A');
                    currentToken.Add('B');

                    currentTokenType = typeof(StringConcatTab);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 2; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 3 && charIndex > 0 && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex - 1])) && inputCodeLine[charIndex] == 'S' && inputCodeLine[charIndex + 1] == 'P' && inputCodeLine[charIndex + 2] == 'C' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 3]))) ||
                    ((inputCodeLine.Length - charIndex) > 3 && inputCodeLine[charIndex] == 'S' && inputCodeLine[charIndex + 1] == 'P' && inputCodeLine[charIndex + 2] == 'C' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 3])))
                    )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('S');
                    currentToken.Add('P');
                    currentToken.Add('C');

                    currentTokenType = typeof(StringConcatSpace);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 2; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 4 && charIndex > 0 && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex - 1])) && inputCodeLine[charIndex] == 't' && inputCodeLine[charIndex + 1] == 'r' && inputCodeLine[charIndex + 2] == 'u' && inputCodeLine[charIndex + 3] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 4]))) ||
                    ((inputCodeLine.Length - charIndex) > 4 && inputCodeLine[charIndex] == 't' && inputCodeLine[charIndex + 1] == 'r' && inputCodeLine[charIndex + 2] == 'u' && inputCodeLine[charIndex + 3] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 4])))
                    )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('t');
                    currentToken.Add('r');
                    currentToken.Add('u');
                    currentToken.Add('e');

                    currentTokenType = typeof(BooleanTrueValue);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 3; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 5 && charIndex > 0 && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex - 1])) && inputCodeLine[charIndex] == 'f' && inputCodeLine[charIndex + 1] == 'a' && inputCodeLine[charIndex + 2] == 'l' && inputCodeLine[charIndex + 3] == 's' && inputCodeLine[charIndex + 4] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 5]))) ||
                    ((inputCodeLine.Length - charIndex) > 5 && inputCodeLine[charIndex] == 'f' && inputCodeLine[charIndex + 1] == 'a' && inputCodeLine[charIndex + 2] == 'l' && inputCodeLine[charIndex + 3] == 's' && inputCodeLine[charIndex + 4] == 'e' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 5])))
                    )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('f');
                    currentToken.Add('a');
                    currentToken.Add('l');
                    currentToken.Add('s');
                    currentToken.Add('e');

                    currentTokenType = typeof(BooleanFalseValue);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 4; // will increment by one once continue line is executed below

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 1 && char.IsWhiteSpace(inputCodeLine[charIndex]) && char.IsDigit(inputCodeLine[charIndex + 1])) ||
                    (char.IsDigit(inputCodeLine[charIndex]))
                    )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add(inputCodeLine[charIndex]);

                    currentTokenType = typeof(NumericValue);

                    currentlyIsANumericValueToken = true;

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 9 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'f' && inputCodeLine[charIndex + 2] == 'u' && inputCodeLine[charIndex + 3] == 'n' && inputCodeLine[charIndex + 4] == 'c' && inputCodeLine[charIndex + 5] == 't' && inputCodeLine[charIndex + 6] == 'i' && inputCodeLine[charIndex + 7] == 'o' && inputCodeLine[charIndex + 8] == 'n' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 9]))) ||
                    ((inputCodeLine.Length - charIndex) > 8 && inputCodeLine[charIndex] == 'f' && inputCodeLine[charIndex + 1] == 'u' && inputCodeLine[charIndex + 2] == 'n' && inputCodeLine[charIndex + 3] == 'c' && inputCodeLine[charIndex + 4] == 't' && inputCodeLine[charIndex + 5] == 'i' && inputCodeLine[charIndex + 6] == 'o' && inputCodeLine[charIndex + 7] == 'n' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 8])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        charIndex = charIndex + 1;
                    }

                    charIndex = charIndex + 7;

                    currentlyIsAFunctionOrClassMethodToken = true;

                    continue;
                }
                else if (inputCodeLine[charIndex] == '.')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add('.');

                    currentTokenType = typeof(Dot);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (
                    ((inputCodeLine.Length - charIndex) > 7 && char.IsWhiteSpace(inputCodeLine[charIndex]) && inputCodeLine[charIndex + 1] == 'r' && inputCodeLine[charIndex + 2] == 'e' && inputCodeLine[charIndex + 3] == 't' && inputCodeLine[charIndex + 4] == 'u' && inputCodeLine[charIndex + 5] == 'r' && inputCodeLine[charIndex + 6] == 'n' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 7]))) ||
                    ((inputCodeLine.Length - charIndex) > 6 && inputCodeLine[charIndex] == 'r' && inputCodeLine[charIndex + 1] == 'e' && inputCodeLine[charIndex + 2] == 't' && inputCodeLine[charIndex + 3] == 'u' && inputCodeLine[charIndex + 4] == 'r' && inputCodeLine[charIndex + 5] == 'n' && (!StringUtils.CharIsLegalNamingCharacter(inputCodeLine[charIndex + 6])))
                     )
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    if (char.IsWhiteSpace(inputCodeLine[charIndex]))
                    {
                        currentToken = new List<char>();
                        currentToken.Add(inputCodeLine[charIndex]);

                        currentTokenType = typeof(WhitespaceCharacter);

                        resultList = GenerateToken(currentToken, currentTokenType, resultList);

                        charIndex = charIndex + 1;
                    }

                    currentToken = new List<char>();
                    currentToken.Add('r');
                    currentToken.Add('e');
                    currentToken.Add('t');
                    currentToken.Add('u');
                    currentToken.Add('r');
                    currentToken.Add('n');

                    currentTokenType = typeof(ReturnKeyword);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    charIndex = charIndex + 5;

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else if (inputCodeLine[charIndex] == ':')
                {
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add(':');

                    currentTokenType = typeof(Colon);

                    resultList = GenerateToken(currentToken, currentTokenType, resultList);

                    currentToken = new List<char>();
                    currentTokenType = typeof(CodeBlock);

                    continue;
                }
                else
                {
                    // at this point, will generate a BasicCodeToken
                    resultList = GenerateToken(currentToken, currentTokenType, resultList); // generates previous token, if applicable

                    currentToken = new List<char>();
                    currentToken.Add(inputCodeLine[charIndex]);

                    currentTokenType = typeof(BasicCodeToken);

                    currentlyIsABasicCodeToken = true;

                    continue;
                }
            }

            resultList = GenerateToken(currentToken, currentTokenType, resultList); // generate last token

            // also generate a NewLineCharacter to append (at the end of this inputCodeLine)
            var newLineCharacter = new NewLineCharacter();
            resultList.Add(newLineCharacter);

            currentlyInCommentBlock = currentlyInCommentBlockCopy;

            return resultList;
        }

        private static List<CodeBlock> GenerateToken(List<char> currentToken, Type currentTokenType, List<CodeBlock> resultList)
        {
            if (currentTokenType == typeof(SwitchKeyword))
            {
                resultList.Add(new SwitchKeyword());
                return resultList;
            }

            if (currentToken.Count == 0)
            {
                return resultList;
            }

            if (currentTokenType == typeof(BasicCodeToken))
            {
                // Note that SuperClass and Class variables will be determined later, in the InputRules (if applicable)
                var basicCodeToken = new BasicCodeToken
                {
                    Value = StringUtils.ConvertCharListToString(currentToken)
                };
                resultList.Add(basicCodeToken);
                return resultList;
            }

            if (currentTokenType == typeof(ClassMethod))
            {
                // Note that SuperClass and Class variables will be determined later, in the InputRules (if applicable)
                var tokenSplitted = StringUtils.ConvertCharListToString(currentToken).Split(new string[] { "::" }, StringSplitOptions.None);

                var classMethod = new ClassMethod
                {
                    ClassName = tokenSplitted[0],
                    MethodName = tokenSplitted[1]
                };

                resultList.Add(classMethod);
                return resultList;
            }

            if (currentTokenType == typeof(FunctionDeclaration))
            {
                var functionDeclaration = new FunctionDeclaration
                {
                    Name = StringUtils.ConvertCharListToString(currentToken)
                };

                resultList.Add(functionDeclaration);
                return resultList;
            }

            if (currentTokenType == typeof(GlobalVariable))
            {
                var globalVar = new GlobalVariable
                {
                    Name = StringUtils.ConvertCharListToString(currentToken)
                };

                resultList.Add(globalVar);
                return resultList;
            }

            if (currentTokenType == typeof(LocalVariable))
            {
                var localVar = new LocalVariable
                {
                    Name = StringUtils.ConvertCharListToString(currentToken)
                };

                resultList.Add(localVar);
                return resultList;
            }

            if (currentTokenType == typeof(NumericValue))
            {
                var numericVal = new NumericValue
                {
                    NumberAsString = StringUtils.ConvertCharListToString(currentToken)
                };

                resultList.Add(numericVal);
                return resultList;
            }

            if (currentTokenType == typeof(SingleLineComment))
            {
                var singleLineComment = new SingleLineComment
                {
                    CodeBlock = StringUtils.ConvertCharListToString(currentToken)
                };

                resultList.Add(singleLineComment);
                return resultList;
            }

            if (currentTokenType == typeof(StringValue))
            {
                var stringVal = new StringValue
                {
                    Val = StringUtils.ConvertCharListToString(currentToken)
                };

                resultList.Add(stringVal);
                return resultList;
            }

            if (currentTokenType == typeof(TakeCodeBlockAsIs))
            {
                var takeCodeBlockAsIs = new TakeCodeBlockAsIs
                {
                    CodeBlock = StringUtils.ConvertCharListToString(currentToken)
                };

                resultList.Add(takeCodeBlockAsIs);
                return resultList;
            }

            if (currentTokenType == typeof(WhitespaceCharacter))
            {
                var whiteSpaceChar = new WhitespaceCharacter
                {
                    WhitespaceChar = currentToken[0]
                };

                resultList.Add(whiteSpaceChar);
                return resultList;
            }

            // if here, simply do a 'basic' token generation (for all other CodeBlock types)
            resultList.Add(GenerateTokenHelperBasic(currentTokenType));

            return resultList;
        }

        private static CodeBlock GenerateTokenHelperBasic(Type tokenType)
        {
            return (CodeBlock)Activator.CreateInstance(tokenType);
        }

        private static string GetSuperclassOrClassValue(string inputCodeLine, int charIndex)
        {
            var idxOfFirstDoubleQuote = inputCodeLine.IndexOf('"', charIndex);
            var idxOfSecondDoubleQuote = inputCodeLine.IndexOf('"', idxOfFirstDoubleQuote + 1);

            return inputCodeLine.Substring(idxOfFirstDoubleQuote + 1, (idxOfSecondDoubleQuote - idxOfFirstDoubleQuote) - 1);
        }

        private static BasicCodeToken GetLastObjectBasicCodeTokenIndex(List<CodeBlock> resultList, List<CodeBlock> currentCompletedCodeBlockList)
        {
            var lastCurrentCodeLineItemsIndex = resultList.Count - 1;
            var lastCompletedCodeBlockItemsIndex = currentCompletedCodeBlockList.Count - 1;

            var idx = lastCurrentCodeLineItemsIndex;

            var previousOpenCurlyBracketCount = 0; // note, will go into 'minuses' if we find ClosedCurlyBrackets along the way
            var previousOpenRoundBracketCount = 0; // note, will go into 'minuses' if we find ClosedRoundBrackets along the way

            while (idx >= 0)
            {
                if (resultList[idx].GetType() == typeof(ClosedCurlyBracket))
                {
                    previousOpenCurlyBracketCount--;
                    idx--;
                    continue;
                }
                else if (resultList[idx].GetType() == typeof(OpenCurlyBracket))
                {
                    previousOpenCurlyBracketCount++;
                    idx--;
                    continue;
                }

                if (previousOpenCurlyBracketCount > 0)
                {
                    if (resultList[idx].GetType() == typeof(WhitespaceCharacter))
                    {
                        idx--;
                        continue;
                    }
                    else if (resultList[idx].GetType() == typeof(ClosedRoundBracket))
                    {
                        previousOpenRoundBracketCount--;
                        idx--;
                        continue;
                    }
                    else if (resultList[idx].GetType() == typeof(OpenRoundBracket))
                    {
                        previousOpenRoundBracketCount++;
                        idx--;
                        continue;
                    }

                    if (previousOpenRoundBracketCount >= 0)
                    {
                        if (resultList[idx].GetType() == typeof(CommentBlockBegin) ||
                            resultList[idx].GetType() == typeof(CommentBlockEnd) ||
                            resultList[idx].GetType() == typeof(SingleLineComment) ||
                            resultList[idx].GetType() == typeof(NewLineCharacter))
                        {
                            idx--;
                            continue;
                        }

                        return (BasicCodeToken)resultList[idx];
                    }
                }

                idx--;
            }

            idx = lastCompletedCodeBlockItemsIndex;

            while (idx >= 0)
            {
                if (currentCompletedCodeBlockList[idx].GetType() == typeof(ClosedCurlyBracket))
                {
                    previousOpenCurlyBracketCount--;
                    idx--;
                    continue;
                }
                else if (currentCompletedCodeBlockList[idx].GetType() == typeof(OpenCurlyBracket))
                {
                    previousOpenCurlyBracketCount++;
                    idx--;
                    continue;
                }

                if (previousOpenCurlyBracketCount > 0)
                {
                    if (currentCompletedCodeBlockList[idx].GetType() == typeof(WhitespaceCharacter))
                    {
                        idx--;
                        continue;
                    }
                    else if (currentCompletedCodeBlockList[idx].GetType() == typeof(ClosedRoundBracket))
                    {
                        previousOpenRoundBracketCount--;
                        idx--;
                        continue;
                    }
                    else if (currentCompletedCodeBlockList[idx].GetType() == typeof(OpenRoundBracket))
                    {
                        previousOpenRoundBracketCount++;
                        idx--;
                        continue;
                    }

                    if (previousOpenRoundBracketCount >= 0)
                    {
                        if (currentCompletedCodeBlockList[idx].GetType() == typeof(CommentBlockBegin) ||
                            currentCompletedCodeBlockList[idx].GetType() == typeof(CommentBlockEnd) ||
                            currentCompletedCodeBlockList[idx].GetType() == typeof(SingleLineComment) ||
                            currentCompletedCodeBlockList[idx].GetType() == typeof(NewLineCharacter))
                        {
                            idx--;
                            continue;
                        }

                        return (BasicCodeToken)currentCompletedCodeBlockList[idx];
                    }
                }

                idx--;
            }

            return null;
        }

        private static List<CodeBlock> SetPhaserObjectTypeOfPreviousToken(List<CodeBlock> resultList, string inputCodeLine, int charIndex, List<CodeBlock> currentCompletedCodeBlockList)
        {
            var resultListOfThisMethod = new List<CodeBlock>();

            var torque2dObjectTypeAsString = inputCodeLine.Substring(charIndex, inputCodeLine.IndexOf('(') - charIndex);

            var idx = resultList.Count - 1;
            var foundPreviousBasicTokenAlready = false;
            var foundPreviousVariableAlready = false;

            while (idx >= 0)
            {
                if (foundPreviousBasicTokenAlready && foundPreviousVariableAlready)
                {
                    resultListOfThisMethod.Insert(0, resultList[idx]);
                    idx--;
                    continue;
                }

                if (resultList[idx].GetType() == typeof(LocalVariable) || resultList[idx].GetType() == typeof(GlobalVariable) || resultList[idx].GetType() == typeof(BasicCodeToken))
                {
                    if (torque2dObjectTypeAsString.ToLower() == Torque2dConstants.SceneClassName.ToLower())
                    {
                        resultList[idx].PhaserObjectType = PhaserObjectType.Scene;
                    }

                    if (torque2dObjectTypeAsString.ToLower() == Torque2dConstants.SpriteClassName.ToLower())
                    {
                        resultList[idx].PhaserObjectType = PhaserObjectType.Sprite;
                    }

                    if (resultList[idx].GetType() == typeof(BasicCodeToken))
                    {
                        foundPreviousBasicTokenAlready = true;
                    }

                    if (resultList[idx].GetType() == typeof(LocalVariable) || resultList[idx].GetType() == typeof(GlobalVariable))
                    {
                        foundPreviousVariableAlready = true;
                    }
                }

                resultListOfThisMethod.Insert(0, resultList[idx]);
                idx--;
            }

            if (!(foundPreviousBasicTokenAlready && foundPreviousVariableAlready))
            {
                idx = currentCompletedCodeBlockList.Count - 1;

                while (idx >= 0)
                {
                    if (currentCompletedCodeBlockList[idx].GetType() == typeof(LocalVariable) || currentCompletedCodeBlockList[idx].GetType() == typeof(GlobalVariable) || currentCompletedCodeBlockList[idx].GetType() == typeof(BasicCodeToken))
                    {
                        if (torque2dObjectTypeAsString.ToLower() == Torque2dConstants.SceneClassName.ToLower())
                        {
                            currentCompletedCodeBlockList[idx].PhaserObjectType = PhaserObjectType.Scene;
                        }

                        if (torque2dObjectTypeAsString.ToLower() == Torque2dConstants.SpriteClassName.ToLower())
                        {
                            currentCompletedCodeBlockList[idx].PhaserObjectType = PhaserObjectType.Sprite;
                        }

                        if (currentCompletedCodeBlockList[idx].GetType() == typeof(BasicCodeToken))
                        {
                            foundPreviousBasicTokenAlready = true;
                        }

                        if (currentCompletedCodeBlockList[idx].GetType() == typeof(LocalVariable) || currentCompletedCodeBlockList[idx].GetType() == typeof(GlobalVariable))
                        {
                            foundPreviousVariableAlready = true;
                        }
                    }

                    if (foundPreviousBasicTokenAlready && foundPreviousVariableAlready)
                    {
                        break;
                    }

                    idx--;
                }
            }

            return resultListOfThisMethod;
        }
    }
}
