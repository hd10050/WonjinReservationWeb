namespace WonjinApi.Models;

// 예약 코드 일별 카운터(M3). code_date(KST)가 PK. 발급은 8-11절의
// INSERT ... ON CONFLICT ... DO UPDATE ... RETURNING 한 문장으로 원자적으로 처리한다.
public class ReservationCodeCounter
{
    public DateOnly CodeDate { get; set; }
    public int LastSeq { get; set; }
}
