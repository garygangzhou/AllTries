using System;

namespace GZ.ActiveDirectoryLibrary.Exceptions
{
    public class ActiveDirectoryException : ApplicationException
    {
        private string _details;
        private ErrorCode _errorCode;

        public ActiveDirectoryException(string message, ErrorCode code, Exception exception) : base(message, exception)
        {
            _errorCode = code;
            _details = exception.Message;
            while (!(exception.InnerException == default))
            {
                _details = _details + ". InnerException: " + exception.InnerException.Message;
                exception = exception.InnerException;
            }
        }

        public ErrorCode ErrorCode
        {
            get
            {
                return _errorCode;
            }

            set
            {
                _errorCode = value;
            }
        }

        public string DetailMessage
        {
            get
            {
                return _details;
            }

            set
            {
                _details = value;
            }
        }

        public static void ReThrowException(Exception ex)
        {
            string errMsg;
            errMsg = ex.Message;
            while (!(ex.InnerException == default))
            {
                errMsg = errMsg + ". InnerException: " + ex.InnerException.Message;
                ex = ex.InnerException;
            }

            if (ex is UnauthorizedAccessException || ex is System.Reflection.TargetInvocationException && !(ex.InnerException == default) && ex.InnerException is UnauthorizedAccessException)
            {
                throw new AccessDeniedException(ex);
            }
            else if (errMsg.IndexOf("password does not meet the password policy requirements") != -1)
            {
                throw new PasswordPolicyException(ex);
            }
            else if (errMsg.IndexOf("specified network password is not correct") != -1)
            {
                throw new PasswordInvalidException(ex);
            }
            else if (errMsg.IndexOf("attribute syntax specified to the directory service is invalid") != -1)
            {
                throw new InvalidAttributeValueException(ex);
            }
            else if (errMsg.IndexOf("search filter is invalid") != -1)
            {
                throw new InvalidSearchFilterException(ex);
            }
            else if (errMsg.IndexOf("There is no such object on the server") != -1)
            {
                throw new InvalidPathException(ex);
            }
            else if (errMsg.IndexOf("Logon failure") != -1)
            {
                throw new LoginFailureException(ex);
            }
            else
            {
                throw new UnknownErrorException(ex);
            }
        }
    }
}