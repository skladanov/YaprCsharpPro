using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public interface IPasswordHasher
    {
        byte[] Hash(string password);

        bool Verify(string password, byte[] storedHash);
    }
}
