using System;

namespace Client.Main.Core.Client;

public partial class CharacterState
{
    private readonly object _configurationLock = new();
    private byte[] _keyConfiguration, _helperConfiguration;
    private int _keyConfigurationRevision;
    public int KeyConfigurationRevision { get { lock (_configurationLock) return _keyConfigurationRevision; } }

    public void ReceiveKeyConfiguration(byte[] data)
    {
        lock (_configurationLock) { _keyConfiguration = (byte[])data.Clone(); _keyConfigurationRevision++; }
    }
    public byte[] GetKeyConfiguration()
    {
        lock (_configurationLock) return _keyConfiguration == null ? null : (byte[])_keyConfiguration.Clone();
    }
    public void ReceiveHelperConfiguration(byte[] data)
    {
        lock (_configurationLock) _helperConfiguration = (byte[])data.Clone();
    }
    public byte[] GetHelperConfiguration()
    {
        lock (_configurationLock) return _helperConfiguration == null ? null : (byte[])_helperConfiguration.Clone();
    }
    private void ResetCharacterConfiguration()
    {
        lock (_configurationLock)
        { _keyConfiguration = _helperConfiguration = null; _keyConfigurationRevision++; }
    }
}
