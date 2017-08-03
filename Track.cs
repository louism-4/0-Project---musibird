using System;
using UnityEngine;
using System.Collections.Generic;

namespace DenisVizigin.Objects3D 
{ 
	public class Track : MonoBehaviour
    {
        private double[] _fluxThresholds;

        private int _segmentsH;
        private float _width;
        private float _depth;
        private float _height;

        private GameObject _instance = new GameObject();

        public Track(float width, float height, float depth, Color color, double[] fluxThresholds)
        {
            _instance.name = "Track";
            _fluxThresholds = fluxThresholds;
            _depth = depth;
            _width = width;
            _height = (fluxThresholds.Length - 1) * height;
            _segmentsH = fluxThresholds.Length - 1;

            var meshRenderer = _instance.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            meshRenderer.material = (Material)Resources.Load("TrackMaterial");
			_instance.AddComponent<Rigidbody>().isKinematic = true;
            Build();
        }

		public static GameObject laser;
		public static GameObject sphereRed;
		public static GameObject sphereOrange;
		public static GameObject sphereYellow;
		public static GameObject sphereGreen;
		public static GameObject sphereBlue;
		public static GameObject spherePurple;
		public static GameObject ring;

		Vector3 posY;
		Vector3 posZ;
		int posCase;
		int sphereCase;
		GameObject theeSphere;
		int laserPosCase;
		int multiple;
		int laserMultiple;

        private void Build()
        {
            Vector3[] vertices = new Vector3[2 * (_segmentsH + 1)];
            Vector3[] normals = new Vector3[2 * (_segmentsH + 1)];
            Vector2[] uvs = new Vector2[2 * (_segmentsH + 1)];
            int[] triangles = new int[6 * _segmentsH];

            float x, y, z;
            float ny, nz;
            float sny, snz, s;

            int numIndices = 0;
            int index = 0;
            int basis;

            sny = snz = 0.0f;
            for (int yi = 0; yi <= _segmentsH; yi++)
            {
                for (int xi = 0; xi <= 1; xi++)
                {
                    x = (xi - 1) * _width;
                    y = (float)_fluxThresholds[_segmentsH - yi] * _depth;
                    z = -((float)yi / (float)_segmentsH - 1) * _height;
                    vertices[index] = new Vector3(x, y, z);

					if (index != 0 && index % 250 == 0) {
						Instantiate (ring, new Vector3 (-512, y, z), Quaternion.Euler(90,0,0));
					}
					multiple = spawnPosZ();
					if(index != 0 && index % multiple == 0){
						posCase = spawnPos();
						sphereCase = spawnSphere();
						theeSphere = (sphereCase == 1) ? sphereRed : (sphereCase == 2) ? sphereOrange :
							(sphereCase == 3) ? sphereYellow : (sphereCase == 4) ? sphereGreen : (sphereCase == 5) ? sphereBlue :
							(sphereCase == 6) ? spherePurple : null;
						switch(posCase){
						case 1: Instantiate(theeSphere, new Vector3(-250, y + 100, z), Quaternion.identity); break;
						case 2: Instantiate(theeSphere, new Vector3(-512, y + 100, z), Quaternion.identity); break;
						case 3: Instantiate(theeSphere, new Vector3(-770, y + 100, z), Quaternion.identity); break;
						}
					}
					if(PlayerPrefs.GetInt("easy",0) == 1){
						if(index != 0 && index % 50 == 0){
							laserPosCase = laserSpawnX();
							switch(laserPosCase){
							case 1: Instantiate(laser, new Vector3(-110, y + 100, z), Quaternion.Euler(0,270,0)); break;
							case 2: Instantiate(laser, new Vector3(-387, y + 100, z), Quaternion.Euler(0,270,0)); break;
							case 3: Instantiate(laser, new Vector3(-686, y + 100, z), Quaternion.Euler(0,270,0)); break;
							}
						}
					} else if (PlayerPrefs.GetInt("hard",0) == 1){
						if(index != 0 && index % 35 == 0){
							laserPosCase = laserSpawnX();
							switch(laserPosCase){
							case 1: Instantiate(laser, new Vector3(-110, y + 100, z), Quaternion.Euler(0,270,0)); break;
							case 2: Instantiate(laser, new Vector3(-387, y + 100, z), Quaternion.Euler(0,270,0)); break;
							case 3: Instantiate(laser, new Vector3(-686, y + 100, z), Quaternion.Euler(0,270,0)); break;
							}
						}
					}

                    if (xi == 0)
                    {
                        ny = -_width * ((((yi + 1) / _segmentsH - 1) * _height) - y);
                        nz = (float) ((yi != 0) ? -_width * (_fluxThresholds[_segmentsH - yi + 1] * _depth - _fluxThresholds[_segmentsH - yi] * _depth) : 1);

						s = (float) Math.Sqrt(ny * ny + nz * nz);
                        sny = Math.Abs(ny / s);
                        snz = nz / s;
                    }

                    normals[index] = new Vector3(0, sny, snz);

                    if (xi == 0 && yi != _segmentsH)
                    {
                        basis = 2 * yi;
                        triangles[numIndices++] = (basis + 1);
                        triangles[numIndices++] = (basis + 2 + 1);
                        triangles[numIndices++] = basis;
                        triangles[numIndices++] = (basis + 2 + 1);
                        triangles[numIndices++] = (basis + 2);
                        triangles[numIndices++] = basis;
                    }

                    uvs[index] = new Vector2(
                         xi * (_width / 512), 
                         ((float)yi / (float)_segmentsH) * _height / 512
                         );
                    index++;
                }
            }

            Mesh mesh = new Mesh
                {
                    vertices = vertices, 
                    triangles = triangles,
                    normals = normals,
                    uv = uvs
                };
            mesh.RecalculateNormals();
            
            var meshFilter = (MeshFilter)_instance.AddComponent(typeof(MeshFilter));
            meshFilter.mesh = mesh;
        }

		private int spawnPos(){
			return UnityEngine.Random.Range(1,4);
		}

		private int spawnPosZ(){
			int rand;
			int value;
			rand = UnityEngine.Random.Range(0,3);
			return value = (rand == 0) ? 50 : (rand == 1) ? 65 : (rand == 2) ? 90 : 0;
		}

		private int spawnSphere(){
			return UnityEngine.Random.Range(1,7);
		}

		private int laserSpawnX(){
			return UnityEngine.Random.Range(1,4);
		}
    }
}
