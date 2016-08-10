#include "libs/hg_sdf.glsl"

uniform vec2 iResolution;
uniform float iGlobalTime;
uniform float iCamPosX;
uniform float iCamPosY;
uniform float iCamPosZ;
uniform float iCamRotX;
uniform float iCamRotY;
uniform float iCamRotZ;

const float epsilon = 0.0001;
const int maxSteps = 128;
vec3 lightPosition = vec3(sin(iGlobalTime), 1.0, cos(iGlobalTime));

float plane(vec3 point, vec3 normal, float d) {
    return dot(point, normal) - d;
}

float sphere(vec3 point, vec3 center, float radius) {
    return length(point - center) - radius;
}

vec3 coordinateRep(vec3 point, vec3 c)
{
    return mod(point, c) - 0.5 * c;
}

float distScene(vec3 point)
{
	float distPlane = plane(point, vec3(0.0, 1.0, 0.0), -0.5);
	point = coordinateRep(point, vec3(1.0, 1.0, 1.0));
	float distSphere = sphere(point, vec3(0.0, 0.0, 0.0), 0.2);
	return min(distPlane, distSphere);
}

//by numerical gradient
vec3 getNormal(vec3 point)
{
	float d = epsilon;
	//get points a little bit to each side of the point
	vec3 right = point + vec3(d, 0.0, 0.0);
	vec3 left = point + vec3(-d, 0.0, 0.0);
	vec3 up = point + vec3(0.0, d, 0.0);
	vec3 down = point + vec3(0.0, -d, 0.0);
	vec3 behind = point + vec3(0.0, 0.0, d);
	vec3 before = point + vec3(0.0, 0.0, -d);
	//calc difference of distance function values == numerical gradient
	vec3 gradient = vec3(distScene(right) - distScene(left),
		distScene(up) - distScene(down),
		distScene(behind) - distScene(before));
	return normalize(gradient);
}

vec3 calcRayDir(vec2 coord, float fov, vec2 resolution)
{
	float tanFov = tan(fov / 2.0 * 3.14159 / 180.0) / resolution.x;
	vec2 p = tanFov * (coord * 2.0 - resolution);
	return normalize(vec3(p.x, p.y, 1.0));
}

void main()
{
	vec3 camP = vec3(iCamPosX, iCamPosY, iCamPosZ);
	vec3 rayDir = calcRayDir(gl_FragCoord.xy, 80.0, iResolution.xy);
	pR(rayDir.yz, iCamRotX);
	pR(rayDir.xz, iCamRotY);

	vec3 point = camP;
	bool objectHit = false;
	float t = 0.0;
    for(int steps = 0; steps < maxSteps; ++steps)
    {
        float dist = distScene(point);
        if(epsilon > dist)
        {
			objectHit = true;
            break;
        }
        t += dist;
        point = camP + t * rayDir;
    }
	vec3 color = vec3(0.0, 0.0, 0.0);
	if(objectHit)
	{
		vec3 lightDir = normalize(lightPosition - point);
		vec3 normal = getNormal(point);
		//shadows
		float lambert = max(0.2 ,dot(normal, lightDir));
		color = lambert * vec3(1.0);
	}
	//fog
	float tmax = 10.0;
	float factor = t/tmax;
	// factor = clamp(factor, 0.0, 1.0);
	color = mix(color, vec3(1.0, 0.8, 0.1), factor);
	
	gl_FragColor = vec4(color, 1.0);
}



