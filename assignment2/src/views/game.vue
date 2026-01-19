<template>
  <v-container class="py-8">
    <v-card class="mx-auto" max-width="600">
      <v-card-title class="text-h5">Wordle</v-card-title>
      <v-card-text>
        This is placeholder text for the game page. The real fun comes later.
      </v-card-text>
      <v-card-actions>
        <v-btn color="primary" @click="goHome">Back to Home</v-btn>
      </v-card-actions>
    </v-card>
  </v-container>
</template>

<script setup lang="ts">
import { useRouter } from "vue-router";

const router = useRouter();
function goHome() {
  router.push("/");
}
</script>
