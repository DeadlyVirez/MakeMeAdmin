namespace SinclairCC.MakeMeAdmin
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// This class stores application data in an encrypted file.
    /// </summary>
    [Serializable]
    [XmlRootAttribute("applicationSettings")]
    public class EncryptedSettings
    {
        /// <summary>
        /// The list of users that have been added to the
        /// local Administrators group.
        /// </summary>
        [XmlArray("addedUsers")]
        [XmlArrayItem("user")]
        public UserList AddedUsers { get; set; }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constructor is used by the serialization process.
        /// </remarks>
        public EncryptedSettings()
        {
            if (this.AddedUsers == null)
            {
                this.AddedUsers = new UserList();
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">
        /// The path of a file containing an XML-serialized EncryptedSettings object.
        /// </param>
        public EncryptedSettings(string filePath)
        {
            if (this.AddedUsers == null)
            {
                this.AddedUsers = new UserList();
            }
            if (System.IO.File.Exists(filePath))
            {
                this.Load(filePath);
            }
            else
            {
                if (this.AddedUsers == null)
                {
                    this.AddedUsers = new UserList();
                }
            }

            try
            {
                RemoveOldUsersFile();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Gets the path of the file in which settings are stored.
        /// </summary>
        public static string SettingsFilePath
        {
            get
            {
                const string EncryptedSettingsFile = "users.xml";
                string filePath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Make Me Admin");
                filePath = System.IO.Path.Combine(filePath, EncryptedSettingsFile);
                return filePath;
            }
        }

        private static string OldSettingsFilePath
        {
            get
            {
                const string EncryptedSettingsFile = "users.xml";
                string filePath = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Make Me Admin");
                filePath = System.IO.Path.Combine(filePath, EncryptedSettingsFile);
                return filePath;
            }
        }

        private static void RemoveOldUsersFile()
        {
            try
            {
                if (System.IO.File.Exists(EncryptedSettings.OldSettingsFilePath))
                {
                    System.IO.File.Delete(EncryptedSettings.OldSettingsFilePath);
                }

                string parentPath = System.IO.Path.GetDirectoryName(EncryptedSettings.OldSettingsFilePath);
                if ((System.IO.Directory.Exists(parentPath)) && (System.IO.Directory.GetFileSystemEntries(parentPath).Length == 0))
                {
                    System.IO.Directory.Delete(parentPath, false);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void RemoveAllSettings()
        {
            try
            {
                if (System.IO.File.Exists(EncryptedSettings.SettingsFilePath))
                {
                    System.IO.File.Delete(EncryptedSettings.SettingsFilePath);
                }

                string parentPath = System.IO.Path.GetDirectoryName(EncryptedSettings.SettingsFilePath);
                if ((System.IO.Directory.Exists(parentPath)) && (System.IO.Directory.GetFileSystemEntries(parentPath).Length == 0))
                {
                    System.IO.Directory.Delete(parentPath, false);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        

        /// <summary>
        /// Adds a user to the list of users that have been added to the Administrators group.
        /// </summary>
        /// <param name="userIdentity">
        /// A WindowsIdentity object representing the user who is to be added to the list.
        /// </param>
        /// <param name="expirationTime">
        /// The date and time at which the user's administrator rights expire.
        /// </param>
        /// <param name="remoteAddress">
        /// The remote host from which the request for administrator rights came.
        /// </param>
        public void AddUser(WindowsIdentity userIdentity, DateTime? expirationTime, string remoteAddress)
        {
            if (this.AddedUsers.Contains(userIdentity.User))
            { // The user is already in the list.
                if (this.AddedUsers[userIdentity.User].ExpirationTime.HasValue)
                {
                    if (expirationTime.HasValue)
                    {
                        // Choose the earlier of the two expiration times.
                        this.AddedUsers[userIdentity.User].ExpirationTime = DateTime.FromFileTime(Math.Min(expirationTime.Value.ToFileTime(), this.AddedUsers[userIdentity.User].ExpirationTime.Value.ToFileTime()));
                    }
                    else
                    { // expirationTime doesn't have a value, but the currently-stored user does.
                        // Err on the side of safety, and keep the currently-stored value.
                    }
                }
                else if (expirationTime.HasValue)
                { // The currently-stored user does not have an expiration value, but expirationTime does.
                    this.AddedUsers[userIdentity.User].ExpirationTime = expirationTime;
                }
            }
            else
            { // The user is not already in the list.
                this.AddedUsers.Add(new User(userIdentity, expirationTime, remoteAddress));
            }
            this.Save();
        }
        
        /// <summary>
        /// Removes a user from the list of added users.
        /// </summary>
        /// <param name="userSecurityIdentifier">
        /// The security identifier (SID) of the user to be removed.
        /// </param>
        public void RemoveUser(SecurityIdentifier userSecurityIdentifier)
        {
            if (this.AddedUsers.Contains(userSecurityIdentifier))
            {
                this.AddedUsers.Remove(userSecurityIdentifier);
                this.Save();
            }
        }

        /// <summary>
        /// Gets the date and time at which the given user's administrator rights expire.
        /// </summary>
        /// <param name="sid">
        /// The security identifier (SID) of the user in question.
        /// </param>
        /// <returns>
        /// Returns a DateTime object indicating when the user's administrator rights expire.
        /// If the user is not in the list of added users, null is returned.
        /// </returns>
        public DateTime? GetExpirationTime(SecurityIdentifier sid)
        {
            if (this.AddedUsers.Contains(sid))
            {
                return this.AddedUsers[sid].ExpirationTime;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets an array of security identifiers (SIDs), each one representing
        /// a user in the list of added users.
        /// </summary>
        [XmlIgnore]
        public SecurityIdentifier[] AddedUserSIDs
        {
            get
            {
                return this.AddedUsers.GetSIDs();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the given user is in the list.
        /// </summary>
        /// <param name="sid">
        /// The security identifier (SID) of the user to be found.
        /// </param>
        /// <returns>
        /// Returns true if the user is in the list of added users.
        /// </returns>
        public bool ContainsSID(SecurityIdentifier sid)
        {
            return this.AddedUsers.Contains(sid);
        }

        /// <summary>
        /// Gets an array of users whose administrator rights have expired.
        /// </summary>
        /// <returns>
        /// Returns an array containing the set of users whose rights have expired.
        /// If the list of users is null, a null array is returned.
        /// </returns>
        public User[] GetExpiredUsers()
        {
            if (this.AddedUsers == null)
            {
                return null;
            }
            else
            {
                return this.AddedUsers.GetExpiredUsers();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the given user's administrator rights
        /// are the result of a remote request.
        /// </summary>
        /// <param name="sid">
        /// The security identifier (SID) of the user in question.
        /// </param>
        /// <returns>
        /// Returns true if the request for administrator rights for the given users
        /// was from a remote host.
        /// </returns>
        public bool IsRemote(SecurityIdentifier sid)
        {
            return this.AddedUsers.IsRemote(sid);
        }

        /// <summary>
        /// Gets an XML serializer for the EncryptedSettings class.
        /// </summary>
        public static XmlSerializer Serializer
        {
            get
            {
                XmlSerializer serializer = new XmlSerializer(typeof(EncryptedSettings));
                return serializer;
            }
        }

        /// <summary>
        /// Saves the settings to the default file path.
        /// </summary>
        private void Save()
        {
            this.Save(EncryptedSettings.SettingsFilePath);
        }

        /// <summary>
        /// Saves the application settings to the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The path of the file to which settings are to be saved.
        /// </param>
        private void Save(string filePath)
        {
            try
            {
                // Serialize the current object to a memory stream.
                System.IO.MemoryStream plaintextStream = new System.IO.MemoryStream();
                System.Xml.XmlTextWriter plaintextWriter = new System.Xml.XmlTextWriter(plaintextStream, System.Text.Encoding.Unicode);
                XmlSerializer serializer = EncryptedSettings.Serializer;
                lock (serializer)
                {
                    plaintextWriter.Indentation = 0;
                    plaintextWriter.Formatting = System.Xml.Formatting.None;
                    serializer.Serialize(plaintextWriter, this);
                }

                // Convert the plaintext memory stream to an array of bytes.
                byte[] plaintextBytes = plaintextStream.ToArray();
                /*plaintextStream.Close();*/
                plaintextStream.Dispose();

                /*plaintextWriter.Close();*/
                plaintextWriter.Dispose();

                // Encrypt the plaintext byte array.
                byte[] ciphertextBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.LocalMachine);

                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
                }

                // Write the encrypted byte array to the file.
                System.IO.FileStream ciphertextStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                ciphertextStream.Write(ciphertextBytes, 0, ciphertextBytes.Length);
                /*ciphertextStream.Close();*/
                ciphertextStream.Dispose();
            }
            catch (System.InvalidOperationException)
            {
                throw;
            }
            
            /*
            // This is the unencrypted version.
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
                System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(filePath, System.Text.Encoding.Unicode);
                XmlSerializer serializer = EncryptedSettings.Serializer;
                lock (serializer)
                {
                    writer.Indentation = 1;
                    writer.IndentChar = '\t';
                    writer.Formatting = System.Xml.Formatting.Indented;
                    serializer.Serialize(writer, this);
                }
                writer.Close();
            }
            catch (System.InvalidOperationException)
            {
                throw;
            }
            */
        }


        /// <summary>
        /// Loads application settings from the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The path of the file from which settings should be loaded.
        /// </param>
        private void Load(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            { // The specified file exists.

                byte[] buffer = new byte[128];
                int bytesRead = int.MinValue;

                // Read the encrypted bytes from the file.
                System.IO.MemoryStream ciphertextMemoryStream = new System.IO.MemoryStream();
                System.IO.FileStream ciphertextFileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                while ((bytesRead = ciphertextFileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ciphertextMemoryStream.Write(buffer, 0, bytesRead);
                }
                ciphertextFileStream.Dispose();
                
                // Convert the encrypted bytes to an array.
                byte[] ciphertextBytes = ciphertextMemoryStream.ToArray();

                byte[] plaintextBytes = null;
                EncryptedSettings deserializedSettings = null;

                try
                {
                    plaintextBytes = System.Security.Cryptography.ProtectedData.Unprotect(ciphertextBytes, null, DataProtectionScope.LocalMachine);
                    // Deserialize the plaintext byte array.
                    System.IO.MemoryStream plaintextStream = new System.IO.MemoryStream(plaintextBytes);
                    XmlTextReader reader = new XmlTextReader(plaintextStream);
                    XmlSerializer serializer = Serializer;
                    lock (serializer)
                    {
                        deserializedSettings = (EncryptedSettings)serializer.Deserialize(reader);
                    }
                    reader.Dispose();
                    plaintextStream.Dispose();
                }
                catch (System.Security.Cryptography.CryptographicException)
                { // The encrypted data seems to be invalid.

                    int fileCounter = 1;
                    string newFilePath = string.Empty;
                    string fileNamePattern = string.Empty;
                    do
                    {
                        if (fileCounter > 1)
                        {
                            fileNamePattern = "{0} - corrupt ({2}) {1}";
                        }
                        else
                        {
                            fileNamePattern = "{0} - corrupt{1}";
                        }
                        newFilePath = string.Format(fileNamePattern, System.IO.Path.GetFileNameWithoutExtension(EncryptedSettings.SettingsFilePath), System.IO.Path.GetExtension(EncryptedSettings.SettingsFilePath), fileCounter);
                        newFilePath = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(EncryptedSettings.SettingsFilePath)), newFilePath);
                        fileCounter++;
                    } while (System.IO.File.Exists(newFilePath));

                    System.IO.File.Move(filePath, newFilePath);
                    ApplicationLog.WriteEvent(string.Format("Invalid user list file found at \"{0}.\" File has been replaced with an empty file. Previously added users will likely not be removed.", filePath), EventID.InvalidUserListFile, System.Diagnostics.EventLogEntryType.Warning);
                    plaintextBytes = null;
                    deserializedSettings = new EncryptedSettings();
                    deserializedSettings.Save(filePath);
                }

                ciphertextMemoryStream.Dispose();

                this.AddedUsers = deserializedSettings.AddedUsers;
            }
            else
            { // The specified file does not exist. Save a new one.
                this.AddedUsers = new UserList();
                this.Save(filePath);
            }
        }
    }
}
