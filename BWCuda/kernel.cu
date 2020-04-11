#include <stdio.h>
#include <inttypes.h>
#include <cstdlib>
#include <iostream>
#include <cuda_runtime.h>

typedef uint8_t byte;
typedef struct pixel { byte subpixels[3]; } pixel;

__global__ void PixelAverage(pixel* pixels)
{
	byte gray = (pixels[threadIdx.x].subpixels[0] + pixels[threadIdx.x].subpixels[1] + pixels[threadIdx.x].subpixels[2]) / 3;
	pixels[threadIdx.x].subpixels[0] = gray;
	pixels[threadIdx.x].subpixels[1] = gray;
	pixels[threadIdx.x].subpixels[2] = gray;
}

__global__ void PixelLuminosity(pixel* pixels)
{
	byte gray = (0.21 * pixels[threadIdx.x].subpixels[0] + 0.72 * pixels[threadIdx.x].subpixels[1] + 0.07 * pixels[threadIdx.x].subpixels[2]);
	pixels[threadIdx.x].subpixels[0] = gray;
	pixels[threadIdx.x].subpixels[1] = gray;
	pixels[threadIdx.x].subpixels[2] = gray;
}

__global__ void PixelLightness(pixel* pixels)
{
	byte max = (pixels[threadIdx.x].subpixels[0] < pixels[threadIdx.x].subpixels[1]) ? pixels[threadIdx.x].subpixels[1] : pixels[threadIdx.x].subpixels[0];
	max = ((max < pixels[threadIdx.x].subpixels[2]) ? pixels[threadIdx.x].subpixels[2] : max);

	byte min = (pixels[threadIdx.x].subpixels[0] > pixels[threadIdx.x].subpixels[1]) ? pixels[threadIdx.x].subpixels[1] : pixels[threadIdx.x].subpixels[0];
	min = ((min > pixels[threadIdx.x].subpixels[2]) ? pixels[threadIdx.x].subpixels[2] : min);

	byte gray = 0.5 * (max + min);
	pixels[threadIdx.x].subpixels[0] = gray;
	pixels[threadIdx.x].subpixels[1] = gray;
	pixels[threadIdx.x].subpixels[2] = gray;
}

int main()
{
	pixel* pixels;
	int n = 2;
	int size = n * sizeof(pixel);
	cudaMallocManaged(&pixels, size);

	pixels[0].subpixels[0] = 42;
	pixels[0].subpixels[1] = 37;
	pixels[0].subpixels[2] = 12;

	pixels[1].subpixels[0] = 236;
	pixels[1].subpixels[1] = 155;
	pixels[1].subpixels[2] = 23;


	PixelLuminosity <<<1, n >>> (pixels);

	cudaDeviceSynchronize();

	std::cout << +pixels[0].subpixels[0] << " " << +pixels[0].subpixels[1] << " " << +pixels[0].subpixels[2] << std::endl;
	std::cout << +pixels[1].subpixels[0] << " " << +pixels[1].subpixels[1] << " " << +pixels[1].subpixels[2] << std::endl;

	cudaFree(pixels);

	return 0;
}