#version 330

//uniform vec3 iMouse;
uniform vec2 iResolution;
uniform float iGlobalTime;
uniform sampler2D tex;
// in vec2 uv;
		
void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
	vec2 uv = fragCoord / iResolution;
	float Frequency = 100.0;
	float Phase = iGlobalTime * 2.0;
	float Amplitude = 0.01;
	uv.y += sin(uv.x * Frequency + Phase) * Amplitude;
	
	vec3 color = texture(tex, uv ).rgb;
	color = color;
	fragColor = vec4(color, 1.0);
}

void main()
{
	mainImage(gl_FragColor, gl_FragCoord.xy);
}