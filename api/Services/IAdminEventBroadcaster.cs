using System.Threading.Channels;

namespace WonjinApi.Services;

// 예약이 확정(Confirmed)될 때 [예약 달력]을 보고 있는 어드민 탭에 "조용히 새로고침"을 트리거하는
// SSE 브로드캐스터. 2026-08-27 스파이크 테스트로 Cloudflare Workers 프록시(server/api/[...].ts)
// 통과·실시간 도달을 확인 완료. 새 예약 접수 알림은 이걸 안 쓰고 별도 웹 푸시(PushSender)로 처리한다
// — 브라우저를 완전히 닫아도 받아야 하는데 SSE는 탭이 열려 있어야만 동작하기 때문(서로 다른 용도).
public interface IAdminEventBroadcaster
{
    ChannelReader<string> Subscribe(out Guid subscriptionId);
    void Unsubscribe(Guid subscriptionId);
    void PublishReservationConfirmed(int reservationId);
}
