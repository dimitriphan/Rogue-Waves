// Minimal reference stubs for the third-party packages the game's mod compiler references (Netcode, Collections,
// uGUI, TextMeshPro, UniTask). Signatures follow the real packages; bodies are never run.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Collections
{
    public struct FixedString64Bytes : IEquatable<FixedString64Bytes>
    {
        public FixedString64Bytes(string source) { }
        public int Length { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public override string ToString() { throw null; }
        public bool Equals(FixedString64Bytes other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static implicit operator FixedString64Bytes(string b) { throw null; }
        public static bool operator ==(FixedString64Bytes a, FixedString64Bytes b) { throw null; }
        public static bool operator !=(FixedString64Bytes a, FixedString64Bytes b) { throw null; }
    }
    public struct FixedString32Bytes : IEquatable<FixedString32Bytes>
    {
        public FixedString32Bytes(string source) { }
        public override string ToString() { throw null; }
        public bool Equals(FixedString32Bytes other) { throw null; }
        public static implicit operator FixedString32Bytes(string b) { throw null; }
    }
    public struct FixedString128Bytes : IEquatable<FixedString128Bytes>
    {
        public FixedString128Bytes(string source) { }
        public override string ToString() { throw null; }
        public bool Equals(FixedString128Bytes other) { throw null; }
        public static implicit operator FixedString128Bytes(string b) { throw null; }
    }
    public struct FixedString512Bytes : IEquatable<FixedString512Bytes>
    {
        public FixedString512Bytes(string source) { }
        public override string ToString() { throw null; }
        public bool Equals(FixedString512Bytes other) { throw null; }
        public static implicit operator FixedString512Bytes(string b) { throw null; }
    }
}

namespace Unity.Netcode
{
    public class NetworkObject : UnityEngine.MonoBehaviour
    {
        public ulong NetworkObjectId { get { throw null; } }
        public bool IsSpawned { get { throw null; } }
        public ulong OwnerClientId { get { throw null; } }
    }
    public abstract class NetworkBehaviour : UnityEngine.MonoBehaviour
    {
        public ulong NetworkObjectId { get { throw null; } }
        public bool IsSpawned { get { throw null; } }
        public bool IsServer { get { throw null; } }
        public bool IsClient { get { throw null; } }
        public bool IsHost { get { throw null; } }
        public bool IsOwner { get { throw null; } }
        public ulong OwnerClientId { get { throw null; } }
        public NetworkObject NetworkObject { get { throw null; } }
        public NetworkManager NetworkManager { get { throw null; } }
        public virtual void OnNetworkSpawn() { }
        public virtual void OnNetworkDespawn() { }
    }
    public partial class NetworkManager : UnityEngine.MonoBehaviour
    {
        public static NetworkManager Singleton { get { throw null; } }
        public bool IsServer { get { throw null; } }
        public bool IsClient { get { throw null; } }
        public bool IsHost { get { throw null; } }
        public ulong LocalClientId { get { throw null; } }
    }
    public abstract class NetworkVariableBase : IDisposable
    {
        public void Dispose() { }
    }
    public class NetworkVariable<T> : NetworkVariableBase
    {
        public delegate void OnValueChangedDelegate(T previousValue, T newValue);
        public OnValueChangedDelegate OnValueChanged;
        public NetworkVariable() { }
        public NetworkVariable(T value) { }
        public virtual T Value { get { throw null; } set { } }
    }
    public enum NetworkListEvent_Type { Add, Insert, Remove, RemoveAt, Value, Clear, Full }
    public struct NetworkListEvent<T>
    {
        public enum EventType : byte { Add, Insert, Remove, RemoveAt, Value, Clear, Full }
        public EventType Type;
        public T Value;
        public T PreviousValue;
        public int Index;
    }
    public class NetworkList<T> : NetworkVariableBase, IEnumerable<T>
    {
        public delegate void OnListChangedDelegate(NetworkListEvent<T> changeEvent);
        public event OnListChangedDelegate OnListChanged;
        public NetworkList() { }
        public IEnumerator<T> GetEnumerator() { throw null; }
        IEnumerator IEnumerable.GetEnumerator() { throw null; }
        public void Add(T item) { }
        public void Clear() { }
        public bool Contains(T item) { throw null; }
        public bool Remove(T item) { throw null; }
        public int Count { get { throw null; } }
        public int IndexOf(T item) { throw null; }
        public void Insert(int index, T item) { }
        public void RemoveAt(int index) { }
        public T this[int index] { get { throw null; } set { } }
    }
    public struct ServerRpcParams { public ServerRpcReceiveParams Receive; }
    public struct ServerRpcReceiveParams { public ulong SenderClientId; }
    public struct ClientRpcParams { }
    public interface INetworkSerializable { }
    public struct BufferSerializer<T> { }
    [AttributeUsage(AttributeTargets.Method)] public class ServerRpcAttribute : Attribute { public bool RequireOwnership = true; }
    [AttributeUsage(AttributeTargets.Method)] public class ClientRpcAttribute : Attribute { }
}

namespace UnityEngine.EventSystems
{
    public abstract class UIBehaviour : UnityEngine.MonoBehaviour { }
}

namespace UnityEngine.UI
{
    public abstract class Graphic : UnityEngine.EventSystems.UIBehaviour
    {
        public virtual UnityEngine.Color color { get { throw null; } set { } }
        public virtual UnityEngine.Material material { get { throw null; } set { } }
        public bool raycastTarget { get { throw null; } set { } }
        public RectTransformStub rectTransform { get { throw null; } }
    }
    public class RectTransformStub { }
    public abstract class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic
    {
        public UnityEngine.Sprite sprite { get { throw null; } set { } }
        public float fillAmount { get { throw null; } set { } }
        public bool preserveAspect { get { throw null; } set { } }
    }
    public class RawImage : MaskableGraphic { public UnityEngine.Texture texture { get { throw null; } set { } } }
    public class Text : MaskableGraphic { public string text { get { throw null; } set { } } }
    public class Selectable : UnityEngine.EventSystems.UIBehaviour { public bool interactable { get { throw null; } set { } } }
    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEngine.Events.UnityEvent { }
        public ButtonClickedEvent onClick { get { throw null; } set { } }
    }
    public class Toggle : Selectable { public bool isOn { get { throw null; } set { } } }
    public class Slider : Selectable { public float value { get { throw null; } set { } } }
    public class Scrollbar : Selectable { }
    public class ScrollRect : UnityEngine.EventSystems.UIBehaviour { public float verticalNormalizedPosition { get { throw null; } set { } } public float horizontalNormalizedPosition { get { throw null; } set { } } }
    public class LayoutElement : UnityEngine.EventSystems.UIBehaviour { }
    public abstract class LayoutGroup : UnityEngine.EventSystems.UIBehaviour { public UnityEngine.RectOffset padding { get { throw null; } set { } } }
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup { public float spacing { get { throw null; } set { } } }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class GridLayoutGroup : LayoutGroup
    {
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Constraint constraint { get { throw null; } set { } }
        public int constraintCount { get { throw null; } set { } }
        public UnityEngine.Vector2 cellSize { get { throw null; } set { } }
        public UnityEngine.Vector2 spacing { get { throw null; } set { } }
    }
    public class ContentSizeFitter : UnityEngine.EventSystems.UIBehaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode horizontalFit { get { throw null; } set { } }
        public FitMode verticalFit { get { throw null; } set { } }
    }
    public class LayoutRebuilder { public static void ForceRebuildLayoutImmediate(UnityEngine.RectTransform layoutRoot) { } }
    public class Mask : UnityEngine.EventSystems.UIBehaviour { }
    public class RectMask2D : UnityEngine.EventSystems.UIBehaviour { }
    public class Outline : UnityEngine.EventSystems.UIBehaviour { }
    public class Shadow : UnityEngine.EventSystems.UIBehaviour { }
    public class Dropdown : Selectable { }
    public class InputField : Selectable { }
    public class CanvasScaler : UnityEngine.EventSystems.UIBehaviour { }
    public class GraphicRaycaster : UnityEngine.MonoBehaviour { }
}

namespace TMPro
{
    public abstract class TMP_Text : UnityEngine.UI.MaskableGraphic { public string text { get { throw null; } set { } } public float fontSize { get { throw null; } set { } } public UnityEngine.Bounds textBounds { get { throw null; } } }
    public class TextMeshProUGUI : TMP_Text { }
    public class TextMeshPro : TMP_Text { }
    public class TMP_InputField : UnityEngine.UI.Selectable { }
    public class TMP_Dropdown : UnityEngine.UI.Selectable { }
    public class TMP_FontAsset : UnityEngine.ScriptableObject { }
}

namespace Cysharp.Threading.Tasks
{
    public enum UniTaskStatus { Pending, Succeeded, Faulted, Canceled }
    public struct UniTask
    {
        public UniTaskStatus Status { get { throw null; } }
    }
    public struct UniTask<T>
    {
        public UniTaskStatus Status { get { throw null; } }
        public Awaiter GetAwaiter() { throw null; }
        public struct Awaiter
        {
            public bool IsCompleted { get { throw null; } }
            public T GetResult() { throw null; }
        }
    }
    public struct UniTaskVoid { }
}

// Added 2026-10-15 for Riptide's sandbox probe (98_Sandbox.cs): named messages. Kept in their own block so the Netcode
// block above stays as other branches edit it.
namespace Unity.Netcode
{
    public partial class NetworkManager
    {
        public CustomMessagingManager CustomMessagingManager { get { throw null; } }
        public event Action<ulong> OnClientConnectedCallback;
    }
    public enum NetworkDelivery { Unreliable, UnreliableSequenced, Reliable, ReliableSequenced, ReliableFragmentedSequenced }
    public class CustomMessagingManager
    {
        public delegate void HandleNamedMessageDelegate(ulong senderClientId, FastBufferReader messagePayload);
        public void RegisterNamedMessageHandler(string name, HandleNamedMessageDelegate callback) { }
        public void UnregisterNamedMessageHandler(string name) { }
        public void SendNamedMessageToAll(string messageName, FastBufferWriter messageStream, NetworkDelivery networkDelivery = NetworkDelivery.ReliableSequenced) { }
    }
    public struct FastBufferWriter : IDisposable
    {
        public FastBufferWriter(int size, Unity.Collections.Allocator allocator, int maxSize = -1) { }
        public void WriteValueSafe(string s, bool oneByteChars = false) { }
        public void Dispose() { }
    }
    public struct FastBufferReader : IDisposable
    {
        public void ReadValueSafe(out string s, bool oneByteChars = false) { throw null; }
        public void Dispose() { }
    }
}
