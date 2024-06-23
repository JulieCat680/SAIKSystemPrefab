// Upgrade NOTE: upgraded instancing buffer 'AmplifySAIKGunTurretDemoSkybox' to new syntax.

// Made with Amplify Shader Editor v1.9.2.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Amplify/SAIKGunTurretDemoSkybox"
{
	Properties
	{
		_SkyColor("Sky Color", Color) = (0.5,0.5,0.5,1)
		_GroundColor("Ground Color", Color) = (0.5,0.5,0.5,1)
		[HDR]_StarColor("Star Color", Color) = (1,1,1,1)
		_HorizonAdjust("Horizon Adjust", Range( 0 , 1)) = 0.5
		_HorizonStrength("Horizon Strength", Float) = 1
		_StarSize("Star Size", Float) = 20
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		AlphaToMask Off
		Cull Back
		ColorMask RGBA
		ZWrite On
		ZTest LEqual
		Offset 0, 0
		
		Pass
		{
			Name "Unlit"

			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform float4 _GroundColor;
			uniform float4 _SkyColor;
			uniform float _StarSize;
			uniform float4 _StarColor;
			UNITY_INSTANCING_BUFFER_START(AmplifySAIKGunTurretDemoSkybox)
				UNITY_DEFINE_INSTANCED_PROP(float, _HorizonAdjust)
#define _HorizonAdjust_arr AmplifySAIKGunTurretDemoSkybox
				UNITY_DEFINE_INSTANCED_PROP(float, _HorizonStrength)
#define _HorizonStrength_arr AmplifySAIKGunTurretDemoSkybox
			UNITY_INSTANCING_BUFFER_END(AmplifySAIKGunTurretDemoSkybox)

			// Implementation from https://cyangamedev.wordpress.com/2019/07/16/voronoi/
			inline float3 voronoi_noise_randomVector (float3 UV, float offset){
				float3x3 m = float3x3(15.27, 47.63, 99.41, 89.98, 95.07, 38.39, 33.83, 51.06, 60.77);
				UV = frac(sin(mul(UV, m)) * 46839.32);
				return float3(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5, sin(UV.z*offset)*0.5+0.5);
			}

			void Voronoi3D_float(float3 UV, float AngleOffset, float CellDensity, out float Out, out float Cells) {
				float3 g = floor(UV * CellDensity);
				float3 f = frac(UV * CellDensity);
				float3 res = float3(8.0, 8.0, 8.0);
			 
				for(int y=-1; y<=1; y++){
					for(int x=-1; x<=1; x++){
						for(int z=-1; z<=1; z++){
							float3 lattice = float3(x, y, z);
							float3 offset = voronoi_noise_randomVector(g + lattice, AngleOffset);
							float3 v = lattice + offset - f;
							float d = dot(v, v);
							
							if(d < res.x){
								res.y = res.x;
								res.x = d;
								res.z = offset.x;
							}else if (d < res.y){
								res.y = d;
							}
						}
					}
				}
			 
				Out = res.x;
				Cells = res.z;
			}
			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.ase_texcoord1 = v.ase_texcoord;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#endif
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float4 texCoord2 = i.ase_texcoord1;
				texCoord2.xy = i.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float _HorizonAdjust_Instance = UNITY_ACCESS_INSTANCED_PROP(_HorizonAdjust_arr, _HorizonAdjust);
				float _HorizonStrength_Instance = UNITY_ACCESS_INSTANCED_PROP(_HorizonStrength_arr, _HorizonStrength);
				float4 lerpResult11 = lerp( _GroundColor , _SkyColor , saturate( ( ( texCoord2.y + _HorizonAdjust_Instance ) * _HorizonStrength_Instance ) ));
				float localVoronoi3D_float1_g9 = ( 0.0 );
				float3 UV1_g9 = texCoord2.xyz;
				float AngleOffset1_g9 = 10.0;
				float CellDensity1_g9 = _StarSize;
				float Value1_g9 = 0.0;
				float Cells1_g9 = 0.0;
				Voronoi3D_float( UV1_g9 , AngleOffset1_g9 , CellDensity1_g9 , Value1_g9 , Cells1_g9 );
				
				
				finalColor = ( lerpResult11 + ( pow( ( 1.0 - saturate( Value1_g9 ) ) , 200.0 ) * _StarColor ) );
				return finalColor;
			}
			
			ENDCG
		}
	}
	
	Fallback Off
}
