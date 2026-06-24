using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/Audio Data")]
public sealed class AudioDataSO : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Nombre descriptivo para reconocer este audio en el Inspector. Ej: 'Paso en pasto', 'Golpe espada', 'Botón UI'.")]
    [SerializeField]
    private string displayName = "Audio";

    [Header("Clips")]
    [Tooltip("Uno o más clips de audio. Si hay más de uno, se elige al azar en cada reproducción para evitar sonido repetitivo. Ideal para pasos, golpes, impactos.")]
    [SerializeField]
    private AudioClip[] clips;

    [Header("Volumen")]
    [Tooltip("Volumen de reproducción. 1 = volumen original del clip.")]
    [SerializeField, Range(0f, 1f)]
    private float volume = 1f;

    [Header("Pitch")]
    [Tooltip("Pitch mínimo. Valores por debajo de 1 bajan el tono.")]
    [SerializeField, Range(0.5f, 2f)]
    private float pitchMin = 0.9f;

    [Tooltip("Pitch máximo. Valores por encima de 1 suben el tono. La variación entre min y max hace que cada sonido sea levemente distinto.")]
    [SerializeField, Range(0.5f, 2f)]
    private float pitchMax = 1.1f;

    [Header("Opciones")]
    [Tooltip("Si está activo, evita que el mismo clip se repita dos veces seguidas cuando hay más de un clip cargado.")]
    [SerializeField]
    private bool avoidConsecutiveRepeat = true;

    public string DisplayName => displayName;
    public float Volume => volume;
    public float RandomPitch => Random.Range(pitchMin, pitchMax);
    public bool HasClips => clips != null && clips.Length > 0;

    private int _lastIndex = -1;

    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        if (!avoidConsecutiveRepeat)
            return clips[Random.Range(0, clips.Length)];

        int index;
        do { index = Random.Range(0, clips.Length); }
        while (index == _lastIndex);
        _lastIndex = index;
        return clips[index];
    }

    private void OnValidate()
    {
        pitchMin = Mathf.Clamp(pitchMin, 0.5f, pitchMax);
        pitchMax = Mathf.Clamp(pitchMax, pitchMin, 2f);
        volume = Mathf.Clamp01(volume);
    }
}
