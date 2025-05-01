#version 330 core

// <<< Changed vColor to Normal
// in vec3 vColor; 
in vec3 Normal;
// <<< Added FragPos
in vec3 FragPos;
in vec2 vTexCoord;

// Output color for the fragment
out vec4 FragColor;

// Uniforms
uniform sampler2D uTexture; // Texture sampler
uniform vec3 objectColor;   // <<< Added: Base color of the object
uniform vec3 lightColor;    // <<< Added: Color of the light source
uniform vec3 lightDir;      // <<< Added: Normalized direction *towards* the light
uniform vec3 viewPos;       // <<< Added: Camera's world position
uniform float shininess;    // <<< Added: Material shininess factor

void main()
{
    // --- Lighting Calculations ---
    
    // Ambient
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor;
    
    // Diffuse 
    vec3 norm = normalize(Normal); // Ensure normal is unit length
    // lightDir should already be normalized CPU-side
    float diff = max(dot(norm, lightDir), 0.0); // Lambertian factor
    vec3 diffuse = diff * lightColor;
    
    // Specular (Basic Phong)
    float specularStrength = 0.5; // How strong the specular highlight is
    vec3 viewDir = normalize(viewPos - FragPos); // Direction from fragment to viewer
    vec3 lightReflectDir = reflect(-lightDir, norm); // Direction light reflects off the surface
    float spec = pow(max(dot(viewDir, lightReflectDir), 0.0), shininess); // <<< Use shininess uniform
    vec3 specular = specularStrength * spec * lightColor; // Calculate specular color component
    
    // Combine results
    vec3 result = (ambient + diffuse + specular) * objectColor; // <<< Added specular to the sum
    
    // Option 1: Modulate texture color with lighting
    FragColor = texture(uTexture, vTexCoord) * vec4(result, 1.0);
    
    // Option 2: Use lighting on the object color, ignore texture for now
    // FragColor = vec4(result, 1.0);
    
    // Option 3: Modulate texture by diffuse+ambient (common)
    // FragColor = vec4(ambient, 1.0) * texture(uTexture, vTexCoord) + vec4(diffuse, 1.0) * texture(uTexture, vTexCoord);

} 