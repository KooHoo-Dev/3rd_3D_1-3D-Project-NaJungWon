using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    public class MapGrid : MonoBehaviour
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        
        public float MaxStepUp;
        public float MaxDropDown;
        
        public Vector2 Origin { get; }
        
        private Tile[,] tiles;

        private MapGrid(int width, int height, float cellsize, Vector2 origin)
        {
            Width = width;
            Height = height;
            CellSize = cellsize;
            Origin = origin;
            
            tiles = new Tile[width, height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    tiles[x, y] = new Tile( new Vector2Int(x,y), TileKind.Walkable);
                }
            } 
        }
        
        public bool InBounds(int x, int y)
        {
            // x가 0보다 크로 Width 작니? (배열이니까 -1 해서 검사해야겠죠?)
            bool inBoundX = (0 <= x) && (x < Width);
            bool inBoundY = (0 <= y) && (y < Height);
            
            return (inBoundX && inBoundY);
        }
        
        public bool InBounds(Vector2Int point)
        {
            return InBounds(point.x, point.y);
        }
        
        public Tile GetTileOrNull(int x, int y)
        {
            // 예외처리
            if (InBounds(x, y) == false) return null;
            return tiles[x, y];

            // 저는 아래표현이 더 좋다고 생각합니다 (개인적으로)
            //return InBounds(x, y) ? tiles[x, y] : null;
        }
        
        public Tile GetTileOrNull(Vector2Int point)
        {
            return GetTileOrNull(point.x, point.y);
        }
        
        // 3. 월드 좌표 기준 -> 타일 좌표로 변환하는 기능
        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - Origin.x) / CellSize);
            int y = Mathf.FloorToInt((worldPosition.y - Origin.y) / CellSize);
            
            // 마무리 처리
            x = Mathf.Clamp(x, 0, Width - 1);
            y = Mathf.Clamp(y, 0, Height - 1);
            return new Vector2Int(x, y);
        }
        
        // 4. 타일좌표 -> 월드 좌표로 변현화는 기능
        public Vector3 CellToWorld(Vector2Int cellIndex)
        {
            // 0.5f = 월드 좌표 중앙 보정값
            float x = Origin.x + (cellIndex.x + 0.5f) * CellSize;
            //float x = Origin.x + (cellIndex.x * CellSize) + (0.5f * CellSize);
            float z = Origin.y + (cellIndex.y + 0.5f) * CellSize;
            //float x = Origin.x + (cellIndex.x * CellSize) + (0.5f * CellSize);
            
            return new Vector3(x, 0, z);
        }

        // 아래의 함수는 타일 정보를 직렬화 하는 메소드가 됩니다
        public List<TileKind> ToKindList()
        {
            List<TileKind> kindList = new List<TileKind>();
            
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    kindList.Add(tiles[x, y].Kind);
                }
            }

            return kindList;
        }
    }
}

