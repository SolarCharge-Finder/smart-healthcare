namespace Doctor.Domain.Entities;

public class Doctor
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; } // references authservice

    public string FullName { get; set; } = "";

    public string Specialization { get; set; } = "";

    public string Hospital { get; set; } = "";

    public decimal ConsultationFee { get; set; }

    public bool IsApproved { get; set; } = false;

    public DateTime CreatedAt { get; set; }
}
