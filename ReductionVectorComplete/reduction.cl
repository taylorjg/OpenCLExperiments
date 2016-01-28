kernel void reductionVector(
	global const float4 *restrict dataIn,
	global float4 *restrict dataOut,
	local float4 *restrict partialSums)
{
	const int globalId = get_global_id(0);
	const int localId = get_local_id(0);
	const int workGroupId = get_group_id(0);
	const int workGroupSize = get_local_size(0);

	partialSums[localId] = dataIn[globalId];

	barrier(CLK_LOCAL_MEM_FENCE);

	for (int i = workGroupSize >> 1; i > 0; i >>= 1)
	{
		if (localId < i)
		{
			partialSums[localId] += partialSums[localId + i];
		}

		barrier(CLK_LOCAL_MEM_FENCE);
	}

	if (localId == 0)
	{
		dataOut[workGroupId] = partialSums[0];
	}
}

kernel void reductionComplete(
	global const float4 *restrict data, 
    local float4 *restrict partialSums,
	global float *restrict sum)
{
	const int localId = get_local_id(0);
	const int workGroupSize = get_local_size(0);

	partialSums[localId] = data[localId];

	barrier(CLK_LOCAL_MEM_FENCE);

	for (int i = workGroupSize >> 1; i > 0; i >>= 1)
	{
		if (localId < i)
		{
			partialSums[localId] += partialSums[localId + i];
		}

		barrier(CLK_LOCAL_MEM_FENCE);
	}

	if (localId == 0)
	{
		*sum = partialSums[0].s0 + partialSums[0].s1 + partialSums[0].s2 + partialSums[0].s3;
	}
}
