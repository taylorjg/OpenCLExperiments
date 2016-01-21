
## Description

Playing around with [OpenCL](https://www.khronos.org/opencl/) from C# using [OpenCL.NET](https://openclnet.codeplex.com/).

* [FpConfig](https://github.com/taylorjg/OpenCLExperiments/tree/master/FpConfig)
    * Dump out device floating point configuration info
        * `CL_DEVICE_SINGLE_FP_CONFIG`
* [RunKernel](https://github.com/taylorjg/OpenCLExperiments/tree/master/RunKernel)
    * Run a simple kernel
* [SaveBinaries](https://github.com/taylorjg/OpenCLExperiments/tree/master/SaveBinaries)
    * Build a `program` and save the CL binaries to files
        * `CL_PROGRAM_BINARY_SIZES`
        * `CL_PROGRAM_BINARIES`
* [WorkGroupInfo](https://github.com/taylorjg/OpenCLExperiments/tree/master/WorkGroupInfo)
    * Dump out kernel work group info
        * `CL_KERNEL_WORK_GROUP_SIZE`
        * `CL_KERNEL_PREFERRED_WORK_GROUP_SIZE_MULTIPLE`
        * `CL_KERNEL_COMPILE_WORK_GROUP_SIZE`
        * `CL_KERNEL_LOCAL_MEM_SIZE`
        * `CL_KERNEL_PRIVATE_MEM_SIZE`
* [ReductionScalar](https://github.com/taylorjg/OpenCLExperiments/tree/master/ReductionScalar)
    * Implement reduction (see section 10.2 _Numerical Reduction_ in _OpenCL in Action_)

## Links

* [OpenCL - The open standard for parallel programming of heterogeneous systems](https://www.khronos.org/opencl/)
* [OpenCL (Wikipedia)](https://en.wikipedia.org/wiki/OpenCL)
* [OpenCL 1.2 Reference Pages](https://www.khronos.org/registry/cl/sdk/1.2/docs/man/xhtml/)
    * _(I am using version 1.2 - current version is 2.1)_
* [OpenCL.NET](https://openclnet.codeplex.com/)
* [OpenCL.NET (NuGet)](https://www.nuget.org/packages/OpenCL.Net/)
* [OpenCL in Action (Manning Publications Co.)](https://www.manning.com/books/opencl-in-action)
* [Simon McIntosh-Smith](https://www.cs.bris.ac.uk/home/simonm/)
    * _Head of the Microelectronics Group and Bristol University Business Fellow_
    * _Senior Lecturer in High Performance Computing and Architectures_
    * [OpenCL: A Hands-on Introduction](https://www.cs.bris.ac.uk/home/simonm/SC13/OpenCL_slides_SC13.pdf)
    * [COMPILING OPENCL KERNELS](http://www.cs.bris.ac.uk/home/simonm/montblanc/AdvancedOpenCL_full.pdf)
* [OpenCL™ Optimization Case Study: Simple Reductions](http://developer.amd.com/resources/documentation-articles/articles-whitepapers/opencl-optimization-case-study-simple-reductions/)
