using UnityEngine;

public class Digger : MonoBehaviour
{
    [SerializeField] Terrain terrain;
    [SerializeField] float brushSize = 3f;
    [SerializeField] float smoothness = 0.5f;
    [SerializeField] float digLength = 15f;
    [SerializeField] float digDepth = 5f;
    [SerializeField] int textureLayerIndex;

    float[,,] originalAlphamaps;
    float[,] originalHeights;

    Vector3 targetPoint;

    bool validTarget;

#if UNITY_EDITOR
    private void Awake()
    {
        originalAlphamaps = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);
        originalHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
    }

    private void OnApplicationQuit()
    {
        terrain.terrainData.SetAlphamaps(0, 0, originalAlphamaps);
        terrain.terrainData.SetHeights(0, 0, originalHeights);
    }
#endif

    private void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        validTarget = false;

        if(Physics.Raycast(ray, out hit, digLength))
        {
            if(hit.collider == null) return;
            if(hit.collider.GetComponent<TerrainCollider>() == null) return;

            validTarget = true;
            targetPoint = hit.point;

            if(Input.GetMouseButtonDown(0))
            {
                DigAtPosition(hit.point, hit.collider.GetComponent<Terrain>());
            }
        }
    }

    private void DigAtPosition(Vector3 worldPosition, Terrain terrainRef)
    {
        TerrainData terrainData = terrainRef.terrainData;

        Vector3 terrainPosition = terrainRef.transform.position;
        Vector3 localPosition = worldPosition - terrainPosition;

        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;
        int heightMapWidth = terrainData.heightmapResolution;
        int heightMapHeight = terrainData.heightmapResolution;
        int alphaMapX = Mathf.RoundToInt((localPosition.x / terrainData.size.x) * (alphaMapWidth - 1));
        int alphaMapY = Mathf.RoundToInt((localPosition.z / terrainData.size.z) * (alphaMapHeight - 1));
        int heightMapX = Mathf.RoundToInt((localPosition.x / terrainData.size.x) * (heightMapWidth - 1));
        int heightMapY = Mathf.RoundToInt((localPosition.z / terrainData.size.z) * (heightMapHeight - 1));
        int brush = Mathf.FloorToInt(brushSize);
        int hXBase = Mathf.Clamp(heightMapX - brush, 0, heightMapWidth - 1);
        int hYBase = Mathf.Clamp(heightMapY - brush, 0, heightMapHeight - 1);
        int hXMax = Mathf.Clamp(heightMapX + brush, 0, heightMapWidth - 1);
        int hYMax = Mathf.Clamp(heightMapY + brush, 0, heightMapHeight - 1);
        int hWidth = (hXMax - hXBase) + 1;
        int hHeight = (hYMax - hYBase) + 1;

        if(hWidth > 0 && hHeight > 0)
        {
            float[,] heights = terrainData.GetHeights(hXBase, hYBase, hWidth, hHeight);

            for(int y = 0; y < hHeight; y++)
            {
                for(int x = 0; x < hWidth; x++)
                {
                    int worldX = hXBase + x;
                    int worldY = hYBase + y;
                    float radius = Vector2.Distance(new Vector2(worldX, worldY), new Vector2(heightMapX, heightMapY));

                    if(radius <= brushSize)
                    {
                        float currentHeight = heights[y, x];
                        float newHeight = Mathf.Lerp(currentHeight, currentHeight - digDepth / terrainData.size.y, 1f - radius / brushSize);
                        heights[y, x] = newHeight;
                    }
                }
            }
            terrainData.SetHeights(hXBase, hYBase, heights);
        }

        int aXBase = Mathf.Clamp(alphaMapX - brush, 0, alphaMapWidth - 1);
        int aYBase = Mathf.Clamp(alphaMapY - brush, 0, alphaMapHeight - 1);
        int aXMax = Mathf.Clamp(alphaMapX + brush, 0, alphaMapWidth - 1);
        int aYMax = Mathf.Clamp(alphaMapY + brush, 0, alphaMapHeight - 1);

        int aWidth = (aXMax - aXBase) + 1;
        int aHeight = (aYMax - aYBase) + 1;

        if(aWidth > 0 && aHeight > 0)
        {
            float[,,] alphaMaps = terrainData.GetAlphamaps(aXBase, aYBase, aWidth, aHeight);

            for(int y = 0; y < aHeight; y++)
            {
                for(int x = 0; x < aWidth; x++)
                {
                    int worldX = aXBase + x;
                    int worldY = aYBase + y;
                    float radius = Vector2.Distance(new Vector2(worldX, worldY), new Vector2(alphaMapX, alphaMapY));

                    if(radius <= brushSize)
                    {
                        for(int layer = 0; layer < terrainData.alphamapLayers; layer++)
                        {
                            float targetAlpha = (layer == textureLayerIndex) ? 1f : 0f;
                            float currentAlpha = alphaMaps[y, x, layer];
                            float smoothnessFactor = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01((radius - brushSize + smoothness) / smoothness));
                            float smoothAlpha = Mathf.Lerp(currentAlpha, targetAlpha, smoothnessFactor);

                            alphaMaps[y, x, layer] = smoothAlpha;
                        }
                    }
                }
            }
            terrainData.SetAlphamaps(aXBase, aYBase, alphaMaps);
        }
    }

    private void OnDrawGizmos()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if(validTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(ray.origin, targetPoint);
            Gizmos.DrawWireSphere(targetPoint, brushSize);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * digLength);
        }
    }
}
