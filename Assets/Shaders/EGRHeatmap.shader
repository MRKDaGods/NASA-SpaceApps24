Shader "EGR/EGRHeatmap" {
    Properties {
        _HeatTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags {"Queue"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha // Alpha blend

        Pass {
            CGPROGRAM
            #pragma vertex vert             
            #pragma fragment frag

            struct vertInput {
                float4 pos : POSITION;
            };  

            struct vertOutput {
                float4 pos : POSITION;
                fixed3 worldPos : TEXCOORD1;
            };

            vertOutput vert(vertInput input) {
                vertOutput o;
                o.pos = UnityObjectToClipPos(input.pos);
                o.worldPos = mul(unity_ObjectToWorld, input.pos).xyz;
                return o;
            }

            uniform int _Points_Length = 0;
            uniform float4 _Points[100];        // (x, y, z) = position
            uniform float4 _Properties[100];    // x = radius, y = intensity
            
            sampler2D _HeatTex;

            half4 frag(vertOutput output) : COLOR {
                // Loops over all the points
                half h = 0;
                for (int i = 0; i < _Points_Length; i ++)
                {
                    // Apply rotation to the point position
                    float4x4 rotationMatrixX = float4x4(
                        1, 0, 0, 0,
                        0, 0, -1, 0,
                        0, 1, 0, 0,
                        0, 0, 0, 1
                    );
                    float3 rotatedPoint = mul(rotationMatrixX, float4(_Points[i].xyz, 1.0)).xyz;

                    // Calculates the contribution of each point
                    half di = distance(output.worldPos, rotatedPoint);

                    half ri = _Properties[i].x;
                    half hi = 1 - saturate(di / ri);

                    h += hi * _Properties[i].y;
                }

                // Converts (0-1) according to the heat texture
                h = saturate(h);
                half4 color = tex2D(_HeatTex, fixed2(h, 0.5));
                return color;
            }
            ENDCG
        }
    } 
    Fallback "Diffuse"
}