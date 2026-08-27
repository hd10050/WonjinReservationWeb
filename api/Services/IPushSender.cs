namespace WonjinApi.Services;

public interface IPushSender
{
    Task SendNewReservationAlertAsync(int reservationId, string customerName, string code);
}
