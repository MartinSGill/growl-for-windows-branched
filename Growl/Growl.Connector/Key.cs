using System;
using System.Collections.Generic;
using System.Text;

namespace Growl.Connector
{
    /// <summary>
    /// Given a password, this class expands the password into an encryption key using a unqiue salt value
    /// and provides means to encrypt and decrypt values using that key.
    /// </summary>
    /// <remarks>
    /// The following procedure is used when converting a password to a key:
    ///     1. The password is converted an UTF8 byte array
    ///     2. A cyptographically secure salt is generated (should be between 4 and 16 bytes)
    ///     3. The salt bytes are appended to the password bytes to form the key basis
    ///     4. The encryption key is generated by computing the hash of the key basis using one of the supported hashing algorithms
    ///     5. The key hash is produced by computing the hash of the encryption key (using the same hashing algorithm used in step 4) and
    ///        hex-encoding it to a fixed-length string
    /// </remarks>
    public class Key
    {
        /// <summary>
        /// A key that represents no password
        /// (Note that this includes no password being set and empty-string passwords)
        /// </summary>
        public static Key None = new Key();

        /// <summary>
        /// The algorithm used when hashing values.
        /// </summary>
        private Cryptography.HashAlgorithmType hashAlgorithm = Cryptography.HashAlgorithmType.MD5;

        /// <summary>
        /// The algorithm used when encrypting values.
        /// </summary>
        private Cryptography.SymmetricAlgorithmType encryptionAlgorithm = Cryptography.SymmetricAlgorithmType.PlainText;

        /// <summary>
        /// The key used for encryption and decryption
        /// </summary>
        private byte[] encryptionKey;

        /// <summary>
        /// The hex-encoded hash of the encryption key
        /// </summary>
        private string keyHash;

        /// <summary>
        /// The original password used to generate the key
        /// </summary>
        private string password;

        /// <summary>
        /// The hex-encoded value of the password salt
        /// </summary>
        private string salt;

        /// <summary>
        /// Creates a new instance of the Key class.
        /// </summary>
        /// <param name="password">The user-supplied password to use as the basis for the key</param>
        /// <param name="hashAlgorithm">The <see cref="Cryptography.HashAlgorithmType"/> used when hashing values</param>
        /// <param name="encryptionAlgorithm">The <see cref="Cryptography.SymmetricAlgorithmType"/> used when encrypting values</param>
        protected Key(string password, Cryptography.HashAlgorithmType hashAlgorithm, Cryptography.SymmetricAlgorithmType encryptionAlgorithm)
        {
            if (!String.IsNullOrEmpty(password))
            {
                this.password = password;
                this.hashAlgorithm = hashAlgorithm;
                this.encryptionAlgorithm = encryptionAlgorithm;

                byte[] saltBytes = Cryptography.GenerateBytes(8);
                this.salt = Cryptography.HexEncode(saltBytes);

                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] keyBasis = new byte[passwordBytes.Length + saltBytes.Length];
                Array.Copy(passwordBytes, 0, keyBasis, 0, passwordBytes.Length);
                Array.Copy(saltBytes, 0, keyBasis, passwordBytes.Length, saltBytes.Length);

                byte[] keyBytes = Cryptography.ComputeHash(keyBasis, hashAlgorithm);
                this.encryptionKey = keyBytes;

                byte[] keyHashBytes = Cryptography.ComputeHash(keyBytes, hashAlgorithm);
                this.keyHash = Cryptography.HexEncode(keyHashBytes);
            }
            else
                InitializeEmptyKey();
        }

        /// <summary>
        /// Creates a new empty key.
        /// </summary>
        private Key()
        {
            InitializeEmptyKey();
        }

        /// <summary>
        /// Initializes the instance with the same values as <see cref="Key.None"/>
        /// </summary>
        private void InitializeEmptyKey()
        {
            this.password = String.Empty;
            this.salt = null;
            this.encryptionKey = null;
            this.keyHash = null;
        }

        /// <summary>
        /// The user-supplied password used as the basis for the key
        /// </summary>
        /// <value>
        /// string
        /// </value>
        public string Password
        {
            get
            {
                return this.password;
            }
        }

        /// <summary>
        /// The hex-encoded value of the password salt.
        /// </summary>
        /// <remarks>
        /// This value will be unique for each instance of <see cref="Key"/>, even
        /// when based on the same password.
        /// </remarks>
        public string Salt
        {
            get
            {
                return this.salt;
            }
        }

        /// <summary>
        /// The key used for encryption and decryption
        /// </summary>
        /// <value>Array of bytes</value>
        protected byte[] EncryptionKey
        {
            get
            {
                return this.encryptionKey;
            }
        }

        /// <summary>
        /// The hex-encoded hash of the encryption key
        /// </summary>
        public string KeyHash
        {
            get
            {
                return this.keyHash;
            }
        }

        /// <summary>
        /// The algorithm used when hashing values
        /// </summary>
        /// <value><see cref="Cryptography.HashAlgorithmType"/></value>
        public Cryptography.HashAlgorithmType HashAlgorithm
        {
            get
            {
                return this.hashAlgorithm;
            }
            protected set
            {
                this.hashAlgorithm = value;
            }
        }

        /// <summary>
        /// The algorithm used when encrypting values
        /// </summary>
        /// <value><see cref="Cryptography.SymmetricAlgorithmType"/></value>
        public Cryptography.SymmetricAlgorithmType EncryptionAlgorithm
        {
            get
            {
                return this.encryptionAlgorithm;
            }
            protected set
            {
                this.encryptionAlgorithm = value;
            }
        }

        /// <summary>
        /// Encrypts the supplied bytes using a random IV.
        /// </summary>
        /// <param name="bytes">The bytes to encrypt</param>
        /// <returns><see cref="EncryptionResult"/></returns>
        public EncryptionResult Encrypt(byte[] bytes)
        {
            EncryptionResult result = Cryptography.Encrypt(this.encryptionKey, bytes, this.encryptionAlgorithm);
            return result;
        }

        /// <summary>
        /// Encrypts the supplied bytes using the supplied IV.
        /// </summary>
        /// <param name="bytes">The bytes to encrypt.</param>
        /// <param name="iv">The IV to use.</param>
        /// <returns><see cref="EncryptionResult"/></returns>
        public EncryptionResult Encrypt(byte[] bytes, ref byte[] iv)
        {
            EncryptionResult result = Cryptography.Encrypt(this.encryptionKey, bytes, this.encryptionAlgorithm, ref iv);
            return result;
        }

        /// <summary>
        /// Decrypts the encrypted bytes
        /// </summary>
        /// <param name="encryptedBytes">The bytes to decrypt.</param>
        /// <param name="iv">The IV to use.</param>
        /// <returns>Array of unencrypted bytes</returns>
        public byte[] Decrypt(byte[] encryptedBytes, byte[] iv)
        {
            byte[] bytes = Cryptography.Decrypt(this.encryptionKey, iv, encryptedBytes, this.encryptionAlgorithm);
            return bytes;
        }

        /// <summary>
        /// Generates a <see cref="Key"/> based on the supplied password and algorithms.
        /// </summary>
        /// <param name="password">The password to use as the basis for the key.</param>
        /// <param name="hashAlgorithm">The <see cref="Cryptography.HashAlgorithmType"/> used when hashing values</param>
        /// <param name="encryptionAlgorithm">The <see cref="Cryptography.SymmetricAlgorithmType"/> used when encrypting values</param>
        /// <returns><see cref="Key"/></returns>
        public static Key GenerateKey(string password, Cryptography.HashAlgorithmType hashAlgorithm, Cryptography.SymmetricAlgorithmType encryptionAlgorithm)
        {
            if (String.IsNullOrEmpty(password))
                return Key.None;
            else
                return new Key(password, hashAlgorithm, encryptionAlgorithm);
        }

        /// <summary>
        /// Compares the provides keyHash and salt to the supplied password to see if they are a match.
        /// </summary>
        /// <param name="password">The password to compare to.</param>
        /// <param name="keyHash">The hex-encoded key hash</param>
        /// <param name="salt">The hex-encoded salt value</param>
        /// <param name="hashAlgorithm">The <see cref="Cryptography.HashAlgorithmType"/> used to generate the key hash</param>
        /// <param name="encryptionAlgorithm">The <see cref="Cryptography.SymmetricAlgorithmType"/> used by this key to do encryption/decryption</param>
        /// <param name="matchingKey">If a match is found, returns the matching <see cref="Key"/>;if no match is found, returns <c>null</c>.</param>
        /// <returns>
        /// <c>true</c> if the key hash and salt match the password;
        /// <c>false</c> otherwise
        /// </returns>
        public static bool Compare(string password, string keyHash, string salt, Cryptography.HashAlgorithmType hashAlgorithm, Cryptography.SymmetricAlgorithmType encryptionAlgorithm, out Key matchingKey)
        {
            matchingKey = null;
            if (!String.IsNullOrEmpty(password))
            {
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] saltBytes = Cryptography.HexUnencode(salt);
                byte[] keyBasis = new byte[passwordBytes.Length + saltBytes.Length];
                Array.Copy(passwordBytes, 0, keyBasis, 0, passwordBytes.Length);
                Array.Copy(saltBytes, 0, keyBasis, passwordBytes.Length, saltBytes.Length);

                byte[] keyBytes = Cryptography.ComputeHash(keyBasis, hashAlgorithm);
                byte[] keyHashBytes = Cryptography.ComputeHash(keyBytes, hashAlgorithm);
                string actualKeyHash = Cryptography.HexEncode(keyHashBytes);

                if (keyHash == actualKeyHash)
                {
                    matchingKey = new Key();
                    matchingKey.password = password;
                    matchingKey.salt = salt;
                    matchingKey.keyHash = keyHash;
                    matchingKey.hashAlgorithm = hashAlgorithm;
                    matchingKey.encryptionKey = keyBytes;
                    matchingKey.encryptionAlgorithm = encryptionAlgorithm;
                    return true;
                }
            }
            return false;
        }

        /*
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (Object.ReferenceEquals(this, obj)) return true;
            Key key = obj as Key;
            if (this.Password.Equals(key.Password)) return true;    // this.Password can never be null
            return false;
        }

        public override int GetHashCode()
        {
            if (this.Password != null) return this.Password.GetHashCode();
            else return 0;
        }

        public static bool operator ==(Key key1, Key key2)
        {
            bool isEqual;
            if (object.ReferenceEquals(key1, null))
            {
                if (object.ReferenceEquals(key2, null))
                {
                    isEqual = true;
                }
                else
                {
                    isEqual = false;
                }
            }
            else
            {
                isEqual = key1.Equals(key2);
            }
            return isEqual;
        }

        public static bool operator !=(Key key1, Key key2)
        {
            return !(key1 == key2);
        }
         * */
    }
}
