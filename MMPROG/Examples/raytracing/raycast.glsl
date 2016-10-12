uniform vec2 iResolution;
uniform float iGlobalTime;

float quad(float a)
{
	return a * a;
}

//M = center of sphere
//r = radius of sphere
//O = origin of ray
//D = direction of ray
float sphere(vec3 M, float r, vec3 O, vec3 D)
{
	vec3 MO = O - M;
	float root = quad(dot(D, MO))- quad(length(D)) * (quad(length(MO)) - quad(r));
	//does ray miss the sphere?
	if(root < 0.001)
	{
		//return something negative
		return -1000.0;
	}
	//ray hits the sphere -> calc t of hit point(s)
	float p = -dot(D, MO);
	float q = sqrt(root);
	//return t of smaller hit point
    return (p - q) > 0.0 ? p - q : p + q;
}

void main()
{
	float fov = 90.0;
	float tanFov = tan(fov / 2.0 * 3.14159 / 180.0) / iResolution.x;
	vec2 p = tanFov * (gl_FragCoord.xy * 2.0 - iResolution.xy);

	vec3 camP = vec3(0.0, 0.0, 0.0);
	vec3 camDir = normalize(vec3(p.x, p.y, 1.0));

	float t = sphere(vec3(0, 0, 1), 0.4, camP, camDir);

	vec3 color;
	if(t < 0)
	{
		color = vec3(0);
	}
	else
	{
		color = vec3(1);
	}
	gl_FragColor = vec4(color, 1.0);
}


