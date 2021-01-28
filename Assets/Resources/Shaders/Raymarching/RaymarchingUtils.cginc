// Operations
float hash(float n ) {
    return frac(sin(n)*753.5453123);
}

float4 noised(in float3 x ) {
    float3 p = floor(x);
    float3 w = frac(x);
    float3 u = w*w*(3.0-2.0*w);
    float3 du = 6.0*w*(1.0-w);
    
    float n = p.x + p.y*157.0 + 113.0*p.z;
    
    float a = hash(n+  0.0);
    float b = hash(n+  1.0);
    float c = hash(n+157.0);
    float d = hash(n+158.0);
    float e = hash(n+113.0);
    float f = hash(n+114.0);
    float g = hash(n+270.0);
    float h = hash(n+271.0);
    
    float k0 =   a;
    float k1 =   b - a;
    float k2 =   c - a;
    float k3 =   e - a;
    float k4 =   a - b - c + d;
    float k5 =   a - c - e + g;
    float k6 =   a - b - e + f;
    float k7 = - a + b + c - d + e - f - g + h;

    return float4( k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z, 
                 du * (float3(k1,k2,k3) + u.yzx*float3(k4,k5,k6) + u.zxy*float3(k6,k4,k5) + k7*u.yzx*u.zxy ));
}

float3 repeat(float3 pos, float3 repetition) {
    return fmod(abs(pos), repetition) - 0.5*repetition;
}

float3 repeatRegular(float3 pos, float regularRepetition) {
    return repeat(pos, float3(regularRepetition, regularRepetition, regularRepetition));
}

float onion(in float sdf, in float thickness) {
    return abs(sdf)-thickness;
}

// Simple Shapes
float boxSDF(float3 pos, float3 bounds) {
    float3 diff = abs(pos) - bounds;
    return length(max(diff, 0.0)) + min(max(diff.x, max(diff.y, diff.z)), 0.0);
}

float cubeSDF(float3 pos, float size) {
    return boxSDF(pos, float3(size,size,size));
}

float sphereSDF(float3 pos, float radius) {
    return length(pos) - radius;
}

// Combination Operations
float unionSDF(float p1, float p2) {
    return min(p1, p2);
}

float intersectionSDF(float p1, float p2) {
    return max(p1, p2);
}

float diffSDF(float p1, float p2) {
    return max(-p2, p1);
}

float smoothIntersectionSDF( float d1, float d2, float k ) {
    float h = clamp( 0.5 - 0.5*(d2-d1)/k, 0.0, 1.0 );
    return lerp( d2, d1, h ) + k*h*(1.0-h);
}

// Complex shapes
float emptyBoxSDF(float3 pos, float3 bounds, float3 innerBounds) {
	float box = boxSDF(pos, bounds);
    float innerBoxVertical = boxSDF(pos, float3(innerBounds.x, bounds.y+1, innerBounds.z));
    float innerBoxHorizontal = boxSDF(pos, float3(bounds.x+1, innerBounds.y, innerBounds.z));
    float innerBoxDepth = boxSDF(pos, float3(innerBounds.x, innerBounds.y, bounds.z+1));

	float innerShape = unionSDF(innerBoxDepth, unionSDF(innerBoxVertical, innerBoxHorizontal));
    
    return diffSDF(box, innerShape);
}

float emptyCubeSDF(float3 pos, float size, float innerSize) {
    return emptyBoxSDF(pos, float3(size,size,size), float3(innerSize,innerSize,innerSize));
}

float octahedronSDF(float3 pos, float size) {
    pos = abs(pos);
    float m = pos.x+pos.y+pos.z-size;
    float3 q;
    if( 3.0*pos.x < m ) q = pos.xyz;
    else if( 3.0*pos.y < m ) q = pos.yzx;
    else if( 3.0*pos.z < m ) q = pos.zxy;
    else return m*0.57735027;
    
    float k = clamp(0.5*(q.z-q.y+size),0.0,size);
    return length(float3(q.x,q.y-size+k,q.z-k));
}