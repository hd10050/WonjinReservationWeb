namespace WonjinApi.Models;

// 예약 ↔ 시술 M:N. 복합 PK (ReservationId, ProcedureId).
public class ReservationProcedure
{
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public int ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }
}
