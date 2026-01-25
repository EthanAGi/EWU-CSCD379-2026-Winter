<template>
  <v-container>
    <h1 class="text-h4">Secret</h1>
    <p class="mt-2">Shh...</p>

    <!-- 🎥 Video wrapper forces vertical layout -->
    <div class="video-wrap mt-6">
      <video class="player" controls autoplay muted playsinline>
        <source src="/videos/secret.mp4" type="video/mp4" />
        Your browser does not support the video tag.
      </video>
    </div>

    <!-- ⬇ Always below the video -->
    <v-btn class="mt-6" variant="outlined" to="/">
      Back Home
    </v-btn>
  </v-container>
</template>

<script setup lang="ts"></script>

<style scoped>
.video-wrap {
  width: 100%;
}

/* Force video to behave like a block element */
.player {
  display: block;
  width: 100%;
  max-width: 720px;
  border-radius: 12px;
}
</style>
