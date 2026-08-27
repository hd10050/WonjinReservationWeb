using System.Collections.Concurrent;
using System.Threading.Channels;

namespace WonjinApi.Services;

// ⚠️ 인메모리 싱글턴 — Render 인스턴스 1개(단일 병원 규모)를 전제로 한다. 여러 인스턴스로 확장하면
// 이 방식으로는 다른 인스턴스에 연결된 클라이언트에게 이벤트가 전달되지 않아 Redis 등 공유 브로커가
// 필요해진다(지금 규모엔 과함 — 2026-08-27 설계 논의에서 합의).
public class AdminEventBroadcaster : IAdminEventBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<string>> _subscribers = new();

    public ChannelReader<string> Subscribe(out Guid subscriptionId)
    {
        subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<string>();
        _subscribers[subscriptionId] = channel;
        return channel.Reader;
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var channel))
            channel.Writer.TryComplete();
    }

    public void PublishReservationConfirmed(int reservationId)
    {
        var payload = reservationId.ToString();
        foreach (var channel in _subscribers.Values)
            channel.Writer.TryWrite(payload);
    }
}
