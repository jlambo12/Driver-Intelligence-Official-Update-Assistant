using DriverGuardian.Domain.Devices;

namespace DriverGuardian.Tests.Unit.Domain;

public sealed class HardwareIdentifierTests
{
    [Fact]
    public void Ctor_ShouldNormalizeToUpperInvariant()
    {
        var id = new HardwareIdentifier("pci\\ven_1234&dev_abcd");

        Assert.Equal("PCI\\VEN_1234&DEV_ABCD", id.Value);
    }

    [Fact]
    public void Ctor_ShouldThrow_WhenValueIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new HardwareIdentifier("  "));
    }
}
