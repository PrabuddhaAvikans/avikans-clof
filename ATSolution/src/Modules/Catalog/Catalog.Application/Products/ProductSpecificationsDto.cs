using System.Text.Json;
namespace Catalog.Application.Products;

public sealed record ProductSpecificationsDto
{
    public decimal? WeightKg { get; init; }
    public string? Dimensions { get; init; }
    public string? Size { get; init; }
    public string? Shape { get; init; }
    public string? Design { get; init; }
    public string? Finish { get; init; }
    public string? Colour { get; init; }
    public string? Glass { get; init; }
    public string? Wiring { get; init; }
    public string? MountingType { get; init; }
    public string? MountingBracket { get; init; }
    public string? Voltage { get; init; }
    public decimal? Wattage { get; init; }
    public string? LedType { get; init; }
    public string? ColorTemperature { get; init; }
    public string? Driver { get; init; }
    public decimal? LengthMm { get; init; }
    public decimal? WidthMm { get; init; }
    public decimal? HeightMm { get; init; }
    public decimal? DiameterMm { get; init; }
    public decimal? LumenOutput { get; init; }
    public decimal? Efficacy { get; init; }
    public string? Cri { get; init; }
    public string? BeamAngle { get; init; }
    public string? IpRating { get; init; }
    public string? InputVoltage { get; init; }
    public decimal? PowerFactor { get; init; }
    public string? Dimming { get; init; }
    public decimal? OpTempMin { get; init; }
    public decimal? OpTempMax { get; init; }
    public decimal? InputPower { get; init; }
    public decimal? InputCurrent { get; init; }
    public string? DriverType { get; init; }
    public string? DriverBrand { get; init; }
    public string? CoatingFinish { get; init; }
    public string? CoatingProcess { get; init; }
    public string? MaterialPrimary { get; init; }
    public string? MaterialSecondary { get; init; }
    public string? Certifications { get; init; }
    public string? Warranty { get; init; }
    public string? ManufacturingNotes { get; init; }
    public IReadOnlyList<ProductAccessoryDto>? Accessories { get; init; }
}
