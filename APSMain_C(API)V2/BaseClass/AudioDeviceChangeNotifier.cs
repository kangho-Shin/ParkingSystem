using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class AudioDeviceChangeNotifier : IMMNotificationClient
    {
        // 장치 연결/해제 시 호출할 이벤트
        public event Action<string, bool>? DevicePluggedStateChanged;

        private readonly MMDeviceEnumerator _deviceEnumerator;

        public AudioDeviceChangeNotifier()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            // 알림 클라이언트를 등록하여 이벤트 수신을 시작합니다.
            _deviceEnumerator.RegisterEndpointNotificationCallback(this);
        }

        // 장치가 연결되거나 해제될 때 이 메서드가 호출됩니다.
        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            // 여기서는 DeviceState.Active 상태가 가장 중요합니다.
            // 연결된 장치는 보통 Active 상태가 됩니다.

            if (newState == DeviceState.Active) {
                // 새 장치가 활성화됨 (이어폰 꽂았을 가능성)
                Console.WriteLine($"장치 활성화: {deviceId}");
                CheckDeviceAndNotify(deviceId, true);
            }
            else if (newState == DeviceState.Disabled || newState == DeviceState.NotPresent) {
                // 장치가 비활성화되거나 사라짐 (이어폰 뽑았을 가능성)
                Console.WriteLine($"장치 비활성화/제거됨: {deviceId}");
                // 장치 ID만으로는 이어폰인지 확인이 어려우므로, 
                // 뽑았다는 것은 기존 출력 장치가 비활성화되었다는 신호로 간주할 수 있습니다.
                CheckDeviceAndNotify(deviceId, false);
            }
        }

        // 이외의 IMMNotificationClient 인터페이스 메서드는 필요에 따라 구현합니다.
        public void OnDeviceAdded(string deviceId) { }
        public void OnDeviceRemoved(string deviceId) { }
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            // 기본 출력 장치가 변경되었을 때 (예: 스피커 -> 이어폰)
            if (flow == DataFlow.Render && role == Role.Multimedia) {
                Console.WriteLine($"기본 출력 장치 변경: {defaultDeviceId}");
                // 새 기본 장치로 출력을 전환하는 로직을 여기에 넣습니다.
                // (이미 오디오 출력을 하고 있다면, 이 새 장치로 재생을 재개해야 합니다.)
            }
        }
        public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }

        // (옵션) 특정 장치 ID가 출력 장치인지 확인하고 이벤트를 발생시키는 헬퍼 메서드
        private void CheckDeviceAndNotify(string deviceId, bool isPlugged)
        {
            try {
                var device = _deviceEnumerator.GetDevice(deviceId);
                // 렌더링 장치(출력)인지 확인하여 이어폰/스피커 관련 장치만 필터링합니다.
                if (device.DataFlow == DataFlow.Render) {
                    // 여기에서 장치 이름(Name)을 통해 "Headphones"나 "Earphones"를 포함하는지
                    // 확인하는 추가 로직을 넣을 수 있습니다.
                    DevicePluggedStateChanged?.Invoke(device.FriendlyName, isPlugged);
                }
            }
            catch (Exception) {
                // 장치에 접근할 수 없는 경우 무시
            }
        }

        public void Dispose()
        {
            // 애플리케이션 종료 시 반드시 호출하여 알림 등록을 해제해야 합니다.
            _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
            _deviceEnumerator.Dispose();
        }
    }
}
